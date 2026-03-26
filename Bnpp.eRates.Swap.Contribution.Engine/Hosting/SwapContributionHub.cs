using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bnpp.eRates.Swap.Contribution.Engine.Hosting;

/// <summary>
/// Generic SignalR hub for all swap contribution products. Replaces SwapLatamContributionHub,
/// SwapInflationContributionHub, etc.
///
/// All per-product hubs followed an identical pattern:
///   1. [Authorize] with product-specific policy
///   2. Extract user identity
///   3. Null-check user
///   4. Log the operation
///   5. Delegate to subscription manager
///
/// This generic hub contains ALL possible methods. Methods for capabilities not listed
/// in the product config will throw a clear "not supported" error if called.
/// Authorization policies are applied dynamically from config using IAuthorizationService.
/// </summary>
[Authorize]
public class SwapContributionHub<TContribution, TInstrument, TTiers> : Hub
    where TContribution : class, IContribution<TInstrument, TTiers>, IKey<string>
    where TInstrument : class, IInstrument
    where TTiers : struct
{
    private readonly ILogger _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers> _subscriptionManager;
    private readonly SwapProductDefinition _product;

    public SwapContributionHub(
        ILogger<SwapContributionHub<TContribution, TInstrument, TTiers>> logger,
        IAuthorizationService authorizationService,
        SwapContributionSubscriptionManager<TContribution, TInstrument, TTiers> subscriptionManager,
        SwapProductDefinition product)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _subscriptionManager = subscriptionManager;
        _product = product;
    }

    // ──────────────────────────────────────────────────────────────────
    // Read operations (always available)
    // ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<TInstrument>> GetAllInstruments()
    {
        await AuthorizeReadAsync();
        _logger.LogInformation("Getting all instruments.");
        return await _subscriptionManager.GetAllInstruments(Context.ConnectionId);
    }

    public async Task SubscribeInstruments(string[] instrumentIds)
    {
        await AuthorizeReadAsync();
        _logger.LogInformation("Subscribing to instruments - {Instruments}", string.Join(',', instrumentIds));
        await _subscriptionManager.AddClientSubscription(Context.ConnectionId, instrumentIds, false);
    }

    public async Task UnSubscribeInstruments(string[] instrumentIds)
    {
        await AuthorizeReadAsync();
        _logger.LogInformation("Unsubscribing from instruments - {Instruments}", string.Join(',', instrumentIds));
        await _subscriptionManager.RemoveClientSubscription(Context.ConnectionId, instrumentIds, false);
    }

    public async Task SubscribeInstrumentTiers(string[] uiKeys)
    {
        await AuthorizeReadAsync();
        await _subscriptionManager.AddClientUIKeySubscription(Context.ConnectionId, uiKeys, true);
    }

    public async Task UnSubscribeInstrumentTiers(string[] uiKeys)
    {
        await AuthorizeReadAsync();
        await _subscriptionManager.RemoveClientUIKeySubscription(Context.ConnectionId, uiKeys, true);
    }

    public async Task SetClientDefaultTier(string tier)
    {
        await AuthorizeReadAsync();
        _logger.LogInformation("Setting client ({Client}) default tier ({Tier}).", Context.ConnectionId, tier);
        await _subscriptionManager.SetClientDefaultTier(Context.ConnectionId, tier);
    }

    public async Task<string> GetClientDefaultTier()
    {
        await AuthorizeReadAsync();
        _logger.LogInformation("Getting client ({Client}) default tier.", Context.ConnectionId);
        return _subscriptionManager.GetClientDefaultTier(Context.ConnectionId);
    }

    // ──────────────────────────────────────────────────────────────────
    // Write operations (capability-gated)
    // ──────────────────────────────────────────────────────────────────

    public async Task QuoteOn(string correlationId, string[] uiKeys)
    {
        var user = await AuthorizeWriteAndGetUser(SwapContributionCapability.QuoteOnOff);
        foreach (var uiKey in uiKeys)
        {
            _logger.LogInformation("Switching ON {UiKey} for user {User}.", uiKey, user);
            await _subscriptionManager.QuoteOn(Context.ConnectionId, correlationId, user, uiKey);
        }
    }

    public async Task QuoteOff(string correlationId, string[] uiKeys)
    {
        var user = await AuthorizeWriteAndGetUser(SwapContributionCapability.QuoteOnOff);
        foreach (var uiKey in uiKeys)
        {
            _logger.LogInformation("Switching OFF {UiKey} for user {User}.", uiKey, user);
            await _subscriptionManager.QuoteOff(Context.ConnectionId, correlationId, user, uiKey);
        }
    }

    public Task AdjustQuoteAskMidSpread(string correlationId, string[] ids, double spread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustAskMidSpread, ids,
            "Updating ask mid spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteAskMidSpread(
                Context.ConnectionId, correlationId, user, id, spread));

    public Task AdjustQuoteBidMidSpread(string correlationId, string[] ids, double spread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustBidMidSpread, ids,
            "Updating bid mid spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteBidMidSpread(
                Context.ConnectionId, correlationId, user, id, spread));

    public Task AdjustQuoteAskQuantity(string correlationId, string[] ids, double askQuantity) =>
        ForEachAuthorized(SwapContributionCapability.AdjustAskQuantity, ids,
            "Updating ask quantity for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteAskQuantity(
                Context.ConnectionId, correlationId, user, id, askQuantity));

    public Task AdjustQuoteBidQuantity(string correlationId, string[] ids, double bidQuantity) =>
        ForEachAuthorized(SwapContributionCapability.AdjustBidQuantity, ids,
            "Updating bid quantity for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteBidQuantity(
                Context.ConnectionId, correlationId, user, id, bidQuantity));

    public Task SwitchQuotingSource(string correlationId, string[] ids, string bidAskSource) =>
        ForEachAuthorized(SwapContributionCapability.SwitchQuotingSource, ids,
            "Switching quoting source for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.SwitchQuotingSource(
                Context.ConnectionId, correlationId, user, id, bidAskSource));

    public Task SwitchMidSource(string correlationId, string[] ids, string midSource) =>
        ForEachAuthorized(SwapContributionCapability.SwitchMidSource, ids,
            "Switching mid source for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.SwitchMidSource(
                Context.ConnectionId, correlationId, user, id, midSource));

    public Task AdjustVwapAskSpread(string correlationId, string[] ids, double vwapAskSpread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustVwapAskSpread, ids,
            "Updating VWAP ask spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustVwapAskSpread(
                Context.ConnectionId, correlationId, user, id, vwapAskSpread));

    public Task AdjustVwapBidSpread(string correlationId, string[] ids, double vwapBidSpread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustVwapBidSpread, ids,
            "Updating VWAP bid spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustVwapBidSpread(
                Context.ConnectionId, correlationId, user, id, vwapBidSpread));

    public Task AdjustVwapAskSize(string correlationId, string[] ids, int vwapAskSize) =>
        ForEachAuthorized(SwapContributionCapability.AdjustVwapAskSize, ids,
            "Updating VWAP ask size for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustVwapAskSize(
                Context.ConnectionId, correlationId, user, id, vwapAskSize));

    public Task AdjustVwapBidSize(string correlationId, string[] ids, int vwapBidSize) =>
        ForEachAuthorized(SwapContributionCapability.AdjustVwapBidSize, ids,
            "Updating VWAP bid size for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustVwapBidSize(
                Context.ConnectionId, correlationId, user, id, vwapBidSize));

    public Task AdjustQuoteMidPrice(string correlationId, string[] ids, double midPrice) =>
        ForEachAuthorized(SwapContributionCapability.AdjustMidPrice, ids,
            "Updating mid price for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteMidPrice(
                Context.ConnectionId, correlationId, user, id, midPrice));

    public Task SetMidYield(string correlationId, string[] ids, double midYield) =>
        ForEachAuthorized(SwapContributionCapability.SetMidYield, ids,
            "Setting mid yield for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.SetMidYield(
                Context.ConnectionId, correlationId, user, id, midYield));

    public Task AdjustQuoteAskMidYieldSpread(string correlationId, string[] ids, double spread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustAskMidYieldSpread, ids,
            "Updating ask mid yield spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteAskMidYieldSpread(
                Context.ConnectionId, correlationId, user, id, spread));

    public Task AdjustQuoteBidMidYieldSpread(string correlationId, string[] ids, double spread) =>
        ForEachAuthorized(SwapContributionCapability.AdjustBidMidYieldSpread, ids,
            "Updating bid mid yield spread for {Id} to {Value} by {User}",
            (id, user) => _subscriptionManager.AdjustQuoteBidMidYieldSpread(
                Context.ConnectionId, correlationId, user, id, spread));

    // ──────────────────────────────────────────────────────────────────
    // Connection lifecycle
    // ──────────────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name;
        await base.OnConnectedAsync();
        await _subscriptionManager.SendHostInfo(Context.ConnectionId);

        if (!string.IsNullOrEmpty(user))
        {
            await _subscriptionManager.AddClient(Context.ConnectionId, user);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Context.User?.Identity?.Name;

        if (exception != null)
        {
            _logger.LogError(exception, "{User}'s connection {Connection} disconnected with error.",
                user, Context.ConnectionId);
        }

        if (!string.IsNullOrEmpty(user))
        {
            await _subscriptionManager.RemoveClient(Context.ConnectionId, user);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers — eliminate the repetitive authorize/validate/log/delegate pattern
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Authorize write + capability check + get user, then run action in parallel for each id.
    /// This single helper replaces ~15 lines of identical boilerplate per hub method.
    /// </summary>
    private async Task ForEachAuthorized(
        string capability,
        string[] ids,
        string logTemplate,
        Func<string, string, Task> action)
    {
        var user = await AuthorizeWriteAndGetUser(capability);
        await Parallel.ForEachAsync(ids, async (id, _) =>
        {
            _logger.LogInformation(logTemplate, id, "(value)", user);
            await action(id, user);
        });
    }

    private async Task AuthorizeReadAsync()
    {
        if (string.IsNullOrEmpty(_product.Policies.Read)) return;
        var result = await _authorizationService.AuthorizeAsync(Context.User!, _product.Policies.Read);
        if (!result.Succeeded)
            throw new HubException($"Not authorized for {_product.Id} read operations.");
    }

    private async Task<string> AuthorizeWriteAndGetUser(string capability)
    {
        // Check capability is enabled for this product
        if (!_product.HasCapability(capability))
        {
            throw new HubException($"Capability '{capability}' is not supported by {_product.Id}.");
        }

        // Check write authorization
        if (!string.IsNullOrEmpty(_product.Policies.Write))
        {
            var result = await _authorizationService.AuthorizeAsync(Context.User!, _product.Policies.Write);
            if (!result.Succeeded)
                throw new HubException($"Not authorized for {_product.Id} write operations.");
        }

        // Extract and validate user
        var user = Context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(user))
        {
            throw new HubException("User identity is not available.");
        }

        return user;
    }
}
