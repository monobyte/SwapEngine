using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.eRates.Web.ConnectionMonitor;
using Bnpp.eRates.Web.Heartbeat;
using Bnpp.eRates.Web.Helper;
using Bnpp.eRates.Web.Market;
using Bnpp.eRates.Web.User;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bnpp.eRates.Swap.Contribution.Engine.Hosting;

/// <summary>
/// Generic subscription manager. Replaces SwapLatamContributionSubscriptionManager,
/// SwapInflationContributionSubscriptionManager, etc.
///
/// All per-product managers were identical: inherit BaseContributionManager,
/// override SendContributionToClient and SendNotification to push via the hub context.
/// The only differences were the type parameters and the event name strings — both now
/// come from configuration.
/// </summary>
public class SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers>
    : BaseContributionManager<SwapContributionUpdate<TContribution>, TContribution, TInstrument, TTiers>
    where TContribution : class, IContribution<TInstrument, TTiers>, IKey<string>
    where TInstrument : class, IInstrument
    where TTiers : struct
{
    private readonly ILogger _logger;
    private readonly IHubContext<SwapContributionHub<TContribution, TInstrument, TTiers>> _hubContext;
    private readonly SwapProductDefinition _product;

    public SwapContributionSubscriptionManager(
        ILogger<SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers>> logger,
        IHubContext<SwapContributionHub<TContribution, TInstrument, TTiers>> hubContext,
        SwapProductDefinition product,
        IContributionProvider<SwapContributionUpdate<TContribution>, TContribution> contributionProvider,
        IInstrumentProvider<TInstrument> instrumentProvider,
        ConnectionManager connectionManager,
        UserMarketProvider userMarketProvider,
        UserInfoProvider userInfoProvider)
        : base(
            logger,
            contributionProvider,
            instrumentProvider,
            connectionManager,
            userMarketProvider,
            userInfoProvider,
            product.DefaultTier,
            product.ThrottleDelay)
    {
        _logger = logger;
        _hubContext = hubContext;
        _product = product;

        // Set endpoint type from config rather than hardcoded enum
        connectionManager.SetEndpointType(product.EndpointType);
    }

    public Task SendHostInfo(string connectionId)
    {
        return _hubContext.Clients.Client(connectionId)
            .SendAsync(HostInfo.HostInfoUpdate, new HostInfo());
    }

    protected override Task SendContributionToClient(string client, TContribution contribution)
    {
        return _hubContext.Clients.Client(client)
            .SendAsync(_product.SignalR.EventContributionUpdate, contribution);
    }

    protected override Task SendNotification(
        string connectionId, string correlationId, string result, object context)
    {
        _logger.LogInformation(
            "Received response for correlation id: {Correlation}, response: {Result}, context: {Context}",
            correlationId, result, context);

        var notification = Conversion.GetHubNotificationFromResult(result);
        notification.Source = $"{_product.Id}ContributionSubscriptionManager";
        notification.Context = context;

        if (notification.CorrelationId != correlationId)
        {
            _logger.LogWarning(
                "Correlation mismatch. Expected: {Expected}, received: {Received}",
                correlationId, notification.CorrelationId);
        }

        return _hubContext.Clients.Client(connectionId)
            .SendAsync(_product.SignalR.EventNotification, notification);
    }
}
