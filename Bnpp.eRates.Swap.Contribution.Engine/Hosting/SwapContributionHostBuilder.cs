using System.Text.Json.Serialization;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.eRates.Swap.Contribution.Engine.Ninject;
using Bnpp.eRates.Web.Configuration;
using Bnpp.eRates.Web.ConnectionMonitor;
using Bnpp.eRates.Web.Heartbeat;
using Bnpp.eRates.Web.Helper;
using Bnpp.eRates.Web.Helper.Extensions;
using Bnpp.eRates.Web.JWTAuthentication.Helper;
using Bnpp.eRates.Web.Market;
using Bnpp.eRates.Web.User;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Ninject;
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
    private Action<IKernel>? _configureNinject;

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
    /// Configure the Ninject kernel before services are resolved.
    /// Use this to load additional Ninject modules required by product-specific
    /// SmartBase dependencies, or to override default bindings.
    /// </summary>
    public SwapContributionApp<TContribution, TInstrument, TTiers> ConfigureNinject(
        Action<IKernel> configure)
    {
        _configureNinject = configure;
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

            // NLog: Setup NLog for Dependency injection
            builder.AddNlog();

            // Global exception handler
            builder.AddExceptionHandling();

            // HTTP header logging (useful for reverse proxy debugging)
            builder.AddHttpLogging();

            builder.AddOpenTelemetry(appConfig, currentEndpoint);
            builder.AddAuthentication(appConfig, currentEndpoint);

            // Authorization policies from config (claim-based, loaded from policysettings.json)
            builder.Services.AddCustomAuthorizationPolicies(appConfig);

            // Health checks (memory + SignalR hub connectivity)
            var signalrHubLocation = $"https://{Environment.MachineName}:{currentEndpoint.Port}{currentEndpoint.Path}";
            var signalrHubEmptyToken = TokenHelper.GetEmptyToken(appConfig);
            if (instance.IsDevelopment())
            {
                signalrHubLocation = $"https://localhost:{currentEndpoint.Port}{currentEndpoint.Path}";
            }
            builder.Services.AddHealthChecks()
                .AddProcessAllocatedMemoryHealthCheck(3072); // 3GB max allocated memory

            // Hosting certificate and port configuration
            builder.AddHostingCertificateConfiguration(logger, appConfig, currentEndpoint);

            // JSON: output enums as strings
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // Feature management (reads "FeatureManagement" config section)
            builder.Services.AddFeatureManagement();

            // Swagger (environment-aware configuration)
            builder.AddSwaggerOptions(instance);

            // SignalR: use Name claim as the user identifier
            // WARNING: Requires that the JWT source ensures the Name claim is unique
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

            // HTTP context + user provider
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<IUserProvider, UserProvider>();

            // ── Phase 4: Engine services ──

            RegisterEngineServices(builder.Services, product, appConfig);

            // ── Phase 5: User overrides ──

            _configureServices?.Invoke(builder.Services);

            // ── Phase 6: Build app and configure pipeline ──

            var app = builder.Build();

            // Log config via ILogger (available via diagnostic source after build)
            builder.Configuration.LogConfig(appConfig, app.Logger);

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
    ///
    /// Architecture: Ninject builds the deep SmartBase/Orion dependency tree
    /// (WireClientFactory, OrionSubscriptionFactory, IDispatcher, etc.) because
    /// these libraries ship with their own Ninject modules. We then bridge the
    /// resolved singletons into MS DI so they're available to ASP.NET services
    /// (Hub, Controller, SubscriptionManager).
    ///
    /// This replaces both the per-product Library.*.Ioc (Ninject module) and
    /// the per-product Host BuilderIoc (Ninject→MS DI bridge).
    /// </summary>
    private void RegisterEngineServices(
        IServiceCollection services,
        SwapProductDefinition product,
        AppConfig appConfig)
    {
        // ── Ninject kernel: SmartBase / Orion dependency tree ──

        var kernel = new StandardKernel();
        kernel.Bind<AppConfig>().ToConstant(appConfig).InSingletonScope();

        // Load JWT Authentication module (common to all products)
        kernel.Load(new Bnpp.eRates.Web.JWTAuthentication.Ioc.Ioc());

        // Load the generic contribution module (replaces per-product Library Ioc)
        kernel.Load(new SwapContributionNinjectModule<TContribution, TInstrument, TTiers>(
            product, appConfig));

        // User Ninject customisation (additional modules, binding overrides)
        _configureNinject?.Invoke(kernel);

        // ── Bridge: Ninject → MS DI ──
        // Keep the kernel alive for the app lifetime
        services.AddSingleton(kernel);

        // Configuration
        services.AddSingleton(product);
        services.AddSingleton(appConfig);

        // SmartBase services (resolved from Ninject kernel)
        services.AddSingleton(kernel.Get<IInstrumentProvider<TInstrument>>());
        services.AddSingleton(
            kernel.Get<IContributionProvider<SwapContributionUpdate<TContribution>, TContribution>>());

        // User info and market (resolved from Ninject)
        services.AddSingleton(kernel.Get<UserInfoProvider>());
        services.AddSingleton(kernel.Get<UserMarketProvider>());

        // ── MS DI native services ──

        // Connection monitoring
        services.AddSingleton<ConnectionMonitorClient>();
        services.AddSingleton<ConnectionManager>();

        // Heartbeat
        services.AddSingleton<HeartbeatProvider>();

        // Subscription manager (needs IHubContext from MS DI + Ninject-bridged providers)
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
