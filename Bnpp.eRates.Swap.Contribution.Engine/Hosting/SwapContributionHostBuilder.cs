using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Feeds;
using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.eRates.Swap.Contribution.Engine.Providers;
using Bnpp.eRates.Web.Configuration;
using Bnpp.eRates.Web.ConnectionMonitor;
using Bnpp.eRates.Web.Heartbeat;
using Bnpp.eRates.Web.Helper;
using Bnpp.eRates.Web.Helper.Extensions;
using Bnpp.eRates.Web.JWTAuthentication.Helper;
using Bnpp.eRates.Web.Market;
using Bnpp.eRates.Web.User;
using Bnpp.SmartBase.Orion.Orion;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Web;

namespace Bnpp.eRates.Swap.Contribution.Engine.Hosting;

/// <summary>
/// Entry point for creating a swap contribution host. Encapsulates ALL the boilerplate
/// that was previously duplicated in every product's Program.cs and Ioc.cs.
///
/// Usage:
///   SwapContributionHostBuilder
///       .Create&lt;SwapLatamContribution, SwapLatamInstrument, SwapTiers&gt;(args)
///       .Run();
/// </summary>
public static class SwapContributionHostBuilder
{
    public static SwapContributionApp<TContribution, TInstrument, TTiers>
        Create<TContribution, TInstrument, TTiers>(string[] args)
        where TContribution : class, ISwapContribution<TInstrument, TTiers>, IKey<string>, IIsDeleted
        where TInstrument : class, ISwapInstrument, IKey<string>, IIsDeleted
        where TTiers : struct
    {
        return new SwapContributionApp<TContribution, TInstrument, TTiers>(args);
    }
}

/// <summary>
/// Configured swap contribution application. Call Run() to start, or use
/// ConfigureServices/ConfigureApp for product-specific overrides before running.
/// </summary>
public class SwapContributionApp<TContribution, TInstrument, TTiers>
    where TContribution : class, ISwapContribution<TInstrument, TTiers>, IKey<string>, IIsDeleted
    where TInstrument : class, ISwapInstrument, IKey<string>, IIsDeleted
    where TTiers : struct
{
    private readonly string[] _args;
    private Action<IServiceCollection>? _configureServices;
    private Action<WebApplication>? _configureApp;

    internal SwapContributionApp(string[] args)
    {
        _args = args;
    }

    /// <summary>
    /// Override or add services before the host is built.
    /// Use this to replace engine defaults with product-specific implementations.
    /// </summary>
    public SwapContributionApp<TContribution, TInstrument, TTiers> ConfigureServices(
        Action<IServiceCollection> configure)
    {
        _configureServices = configure;
        return this;
    }

    /// <summary>
    /// Configure the WebApplication after it's built but before it runs.
    /// Use this to add custom middleware, endpoints, or other app-level config.
    /// </summary>
    public SwapContributionApp<TContribution, TInstrument, TTiers> ConfigureApp(
        Action<WebApplication> configure)
    {
        _configureApp = configure;
        return this;
    }

    /// <summary>
    /// Build and run the host. This replaces the entire per-product Program.cs.
    /// </summary>
    public void Run()
    {
        // ── Phase 1: Bootstrap — read product config to resolve the instance ──

        var bootstrapConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var product = bootstrapConfig.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>()
            ?? throw new InvalidOperationException(
                $"Missing '{SwapProductDefinition.SectionName}' section in appsettings.json");

        ValidateProductConfig(product);

        var instance = AppInstance.GetCurrentInstance(_args, product.InstanceEnvVar);
        GlobalDiagnosticsContext.Set("instance", instance.InstanceName);

        var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
        logger.Debug($"Initializing {product.Id} contribution host");

        try
        {
            logger.Info("================ Server Starting ================");
            logger.Info($"Starting as: {Environment.UserDomainName}\\{Environment.UserName}");
            logger.Info($"Product: {product.Id}");
            logger.Info($"Instance: {instance}");

            // ── Phase 2: Build WebApplication ──

            var builder = WebApplication.CreateBuilder(_args);
            builder.Environment.EnvironmentName = instance.EnvironmentName;
            builder.Configuration.AddCustomConfig(instance, _args);

            // Re-read product config with full environment-specific overrides applied
            product = builder.Configuration.GetSection(SwapProductDefinition.SectionName)
                .Get<SwapProductDefinition>()
                ?? throw new InvalidOperationException("Product configuration missing after env override");

            var appConfig = builder.Configuration.BindConfig();
            builder.Configuration.LogConfig(appConfig, logger);

            var currentEndpoint = appConfig.GetEndpoint(product.Endpoint);

            // ── Phase 3: Standard ASP.NET services ──

            builder.Services.AddCors();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();

            builder.AddNlog();
            builder.AddExceptionHandling();
            builder.AddHttpLogging();
            builder.AddOpenTelemetry(appConfig, currentEndpoint);
            builder.AddAuthentication(appConfig, currentEndpoint);

            // ── Phase 4: Engine services ──

            RegisterEngineServices(builder.Services, product, appConfig);

            // ── Phase 5: User overrides ──

            _configureServices?.Invoke(builder.Services);

            // ── Phase 6: Build app and configure pipeline ──

            var app = builder.Build();

            app.UseMiddleware<HostnameInfoMiddleware>();
            app.UseAuthentication();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();

            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials());

            app.MapControllers();
            app.MapCustomHealthChecks();
            app.UseRouting();
            app.UseAuthorization();

            // ── Phase 7: Initialize subscription manager and map hub ──

            // Eagerly resolve to trigger feed subscriptions
            _ = app.Services.GetRequiredService<
                SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers>>();

            app.MapHub<SwapContributionHub<TContribution, TInstrument, TTiers>>(
                currentEndpoint.Path,
                options => { options.AllowStatefulReconnects = true; });

            _configureApp?.Invoke(app);

            app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// Register all engine services into the MS DI container.
    /// This replaces both the Ninject module (Library.*.Ioc) and the Host's BuilderIoc.
    /// </summary>
    private static void RegisterEngineServices(
        IServiceCollection services,
        SwapProductDefinition product,
        AppConfig appConfig)
    {
        // Configuration
        services.AddSingleton(product);
        services.AddSingleton(appConfig);

        // Instrument feed (single per product)
        services.AddSingleton<ISwapInstrumentFeed<TInstrument>>(sp =>
        {
            var config = appConfig.GetConnection(product.Feeds.Instruments.ConnectionKey);
            return new GenericSwapInstrumentFeed<TInstrument>(
                config,
                product.Id,
                sp.GetRequiredService<WireClientFactory>(),
                sp.GetRequiredService<OrionFeedMonitorFeed>(),
                sp.GetRequiredService<OrionSubscriptionFactory>());
        });

        // Instrument provider
        services.AddSingleton<IInstrumentProvider<TInstrument>>(sp =>
            new GenericSwapInstrumentProvider<TInstrument>(
                sp.GetRequiredService<IDispatcher>(),
                sp.GetRequiredService<ISwapInstrumentFeed<TInstrument>>()));

        // Contribution feeds (config-driven array — 1 for Latam, 4 for Inflation, etc.)
        services.AddSingleton<IContributionFeed<TInstrument, TTiers>[]>(sp =>
        {
            var instrumentProvider = sp.GetRequiredService<IInstrumentProvider<TInstrument>>();
            return product.Feeds.Contributions.Select(feedConfig =>
            {
                var orionConfig = appConfig.GetConnection(feedConfig.ConnectionKey);
                return (IContributionFeed<TInstrument, TTiers>)
                    new GenericSwapContributionFeed<TContribution, TInstrument, TTiers>(
                        orionConfig,
                        sp.GetRequiredService<WireClientFactory>(),
                        sp.GetRequiredService<OrionFeedMonitorFeed>(),
                        sp.GetRequiredService<OrionSubscriptionFactory>(),
                        sp.GetRequiredService<IOrionClientFactory>(),
                        sp.GetRequiredService<IOrionFunctionCall>(),
                        instrumentProvider);
            }).ToArray();
        });

        // Contribution provider
        services.AddSingleton<IContributionProvider<SwapContributionUpdate<TContribution>, TContribution>>(sp =>
            new GenericSwapContributionProvider<TContribution, TInstrument, TTiers>(
                sp.GetRequiredService<IDispatcher>(),
                sp.GetRequiredService<IContributionFeed<TInstrument, TTiers>[]>()));

        // Connection monitoring
        services.AddSingleton<ConnectionMonitorClient>();
        services.AddSingleton<ConnectionManager>();

        // User services
        services.AddSingleton<UserInfoProvider>();
        services.AddSingleton<UserMarketProvider>();

        // Heartbeat
        services.AddSingleton<HeartbeatProvider>();

        // Subscription manager (the core coordination service)
        services.AddSingleton<
            SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers>>();
    }

    private static void ValidateProductConfig(SwapProductDefinition product)
    {
        if (string.IsNullOrWhiteSpace(product.Id))
            ThrowHelper.ThrowArgumentException(nameof(product.Id), "Product.Id is required");
        if (string.IsNullOrWhiteSpace(product.InstanceEnvVar))
            ThrowHelper.ThrowArgumentException(nameof(product.InstanceEnvVar), "Product.InstanceEnvVar is required");
        if (string.IsNullOrWhiteSpace(product.Endpoint))
            ThrowHelper.ThrowArgumentException(nameof(product.Endpoint), "Product.Endpoint is required");
        if (product.Feeds.Contributions.Count == 0)
            ThrowHelper.ThrowArgumentException(nameof(product.Feeds), "At least one contribution feed is required");
        if (string.IsNullOrWhiteSpace(product.Feeds.Instruments.ConnectionKey))
            ThrowHelper.ThrowArgumentException(nameof(product.Feeds.Instruments), "Instrument feed connection is required");
    }
}
