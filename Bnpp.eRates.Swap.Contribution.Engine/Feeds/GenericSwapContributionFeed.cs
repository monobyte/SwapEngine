using System.Linq.Expressions;
using Bnpp.eRates.Contribution.Library.Common;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.SmartBase.Orion.Orion;

namespace Bnpp.eRates.Swap.Contribution.Engine.Feeds;

/// <summary>
/// Generic contribution feed. Replaces SwapLatamContributionFeed, SwapInflationContributionFeed, etc.
/// All per-product feeds had identical structure — inherit SwapContributionFeedBase, override
/// CreateContribution to construct the product type and attach the instrument.
/// This generic version does exactly that for any TContribution type.
/// </summary>
internal class GenericSwapContributionFeed<TContribution, TInstrument, TTiers>
    : SwapContributionFeedBase<TInstrument, TTiers>
    where TContribution : class, ISwapContribution<TInstrument, TTiers>
    where TInstrument : ISwapInstrument
    where TTiers : struct
{
    /// <summary>
    /// Compiled factory: calls TContribution(string, IDictionary&lt;string, object&gt;)
    /// with no per-call reflection overhead.
    /// </summary>
    private static readonly Func<string, IDictionary<string, object>, TContribution> _factory = BuildFactory();

    public GenericSwapContributionFeed(
        Web.Helper.OrionConfig orionConfig,
        WireClientFactory wireClientFactory,
        OrionFeedMonitorFeed orionFeedMonitorFeed,
        OrionSubscriptionFactory orionSubscriptionFactory,
        IOrionClientFactory orionClientFactory,
        IOrionFunctionCall orionFunctionCall,
        IInstrumentProvider<TInstrument> instrumentProvider)
        : base(
            orionConfig,
            orionConfig.Key,
            $"Subscribe to {typeof(TContribution).Name} contributions via Orion",
            wireClientFactory,
            orionFeedMonitorFeed,
            orionSubscriptionFactory,
            orionClientFactory,
            orionFunctionCall,
            instrumentProvider)
    {
    }

    protected override ISwapContribution<TInstrument, TTiers> CreateContribution(
        string orionKey, IDictionary<string, object> items)
    {
        var contrib = _factory(orionKey, items);
        contrib.Instrument = GetInstrument(contrib.InstrumentId);
        return contrib;
    }

    private static Func<string, IDictionary<string, object>, TContribution> BuildFactory()
    {
        var ctor = typeof(TContribution).GetConstructor(
            new[] { typeof(string), typeof(IDictionary<string, object>) })
            ?? throw new InvalidOperationException(
                $"{typeof(TContribution).Name} must have a (string, IDictionary<string, object>) constructor");

        var orionKeyParam = Expression.Parameter(typeof(string), "orionKey");
        var itemsParam = Expression.Parameter(typeof(IDictionary<string, object>), "items");
        var newExpr = Expression.New(ctor, orionKeyParam, itemsParam);
        return Expression.Lambda<Func<string, IDictionary<string, object>, TContribution>>(
            newExpr, orionKeyParam, itemsParam).Compile();
    }
}
