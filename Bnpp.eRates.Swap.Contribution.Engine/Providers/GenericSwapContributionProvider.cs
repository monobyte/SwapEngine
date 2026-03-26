using Bnpp.eRates.Contribution.Library.Common;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.SmartBase.AppBase.Contracts;

namespace Bnpp.eRates.Swap.Contribution.Engine.Providers;

/// <summary>
/// Generic contribution provider. Replaces SwapLatamContributionProvider, SwapInflationContributionProvider, etc.
/// All per-product providers were identical: inherit ContributionProviderBase, override GetUpdate.
/// </summary>
internal class GenericSwapContributionProvider<TContribution, TInstrument, TTiers>
    : ContributionProviderBase<TInstrument, SwapContributionUpdate<TContribution>, TContribution, TTiers>
    where TInstrument : IInstrument
    where TContribution : class, IContribution<TInstrument, TTiers>, IKey<string>
    where TTiers : struct
{
    public GenericSwapContributionProvider(
        IDispatcher dispatcher,
        IContributionFeed<TInstrument, TTiers>[] feeds)
        : base(dispatcher, feeds)
    {
    }

    protected override SwapContributionUpdate<TContribution> GetUpdate(
        TContribution contrib, TContribution? prevContrib)
    {
        return new SwapContributionUpdate<TContribution>(contrib, prevContrib);
    }
}
