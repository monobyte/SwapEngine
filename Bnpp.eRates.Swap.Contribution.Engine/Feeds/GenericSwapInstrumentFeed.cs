using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Web.Helper;
using Bnpp.Gfit.Wire.Extensions.Interfaces;
using Bnpp.SmartBase.AppBase.Contracts;
using Bnpp.SmartBase.Orion.Orion;
using OrionConfig = Bnpp.SmartBase.Orion.Orion.OrionConfig;

namespace Bnpp.eRates.Swap.Contribution.Engine.Feeds;

/// <summary>
/// Generic instrument feed. Replaces SwapLatamInstrumentFeed, SwapInflationInstrumentFeed, etc.
/// Constructs TInstrument instances from Orion dictionaries using a compiled factory delegate
/// (zero reflection overhead per item after startup).
/// </summary>
internal class GenericSwapInstrumentFeed<TInstrument> : ISwapInstrumentFeed<TInstrument>
    where TInstrument : class, ISwapInstrument, IIsDeleted
{
    private readonly string _name;
    private readonly string _description;
    private ISubscription? _subscription;
    private readonly OrionConfig _orionConfig;
    private readonly ISelfRecoveringWireClient _wireClient;
    private readonly OrionSubscriptionFactory _orionSubscriptionFactory;
    private readonly string _whereClause;

    /// <summary>Compiled factory: calls TInstrument(IDictionary&lt;string, object&gt;) with no per-call reflection.</summary>
    private static readonly Func<IDictionary<string, object>, TInstrument> _factory = BuildFactory();

    public GenericSwapInstrumentFeed(
        Web.Helper.OrionConfig config,
        string productId,
        WireClientFactory wireClientFactory,
        OrionFeedMonitorFeed orionFeedMonitorFeed,
        OrionSubscriptionFactory orionSubscriptionFactory)
    {
        _name = $"{productId} Instrument Feed";
        _description = $"Subscribe to {productId} instruments via Orion";

        _orionConfig = new OrionConfig(config.ConnectionStringElements, config.PreferLocalEndPoint);
        _whereClause = config.Filter;
        _orionSubscriptionFactory = orionSubscriptionFactory;

        ISubject<IConnectionState> cs;
        (_wireClient, cs) = wireClientFactory.GetWireClient(_orionConfig);

        var enrichedConnectionState = new EnrichedConnectionState(cs, _name, _description, _orionConfig, _wireClient);
        orionFeedMonitorFeed.AddFeed(enrichedConnectionState.Subject);
    }

    public ISubscription SubscribeToInstruments(
        Action<TInstrument> onAdd,
        Action<TInstrument>? onUpdate = null,
        Action<string>? onRemove = null,
        Action? onRemoveAll = null)
    {
        Debug.Assert(_subscription == null, "Instrument feed already subscribed");

        _subscription = _orionSubscriptionFactory.Subscribe(
            _wireClient,
            _orionConfig.ServiceId,
            _orionConfig.TableName,
            _whereClause,
            d => _factory(d),
            onAdd,
            onUpdate,
            onRemove,
            onRemoveAll);

        return _subscription;
    }

    /// <summary>
    /// Builds a compiled expression tree that directly invokes the TInstrument(IDictionary) constructor.
    /// This avoids Activator.CreateInstance overhead on every Orion data update.
    /// </summary>
    private static Func<IDictionary<string, object>, TInstrument> BuildFactory()
    {
        var ctor = typeof(TInstrument).GetConstructor(new[] { typeof(IDictionary<string, object>) })
            ?? throw new InvalidOperationException(
                $"{typeof(TInstrument).Name} must have a constructor accepting IDictionary<string, object>");

        var param = Expression.Parameter(typeof(IDictionary<string, object>), "items");
        var newExpr = Expression.New(ctor, param);
        return Expression.Lambda<Func<IDictionary<string, object>, TInstrument>>(newExpr, param).Compile();
    }
}
