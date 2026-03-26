using Bnpp.eRates.Contribution.Model.Common;

namespace Bnpp.eRates.Swap.Contribution.Engine.Model;

/// <summary>
/// Generic contribution update wrapper. Replaces ALL per-product update classes
/// (SwapLatamContributionUpdate, SwapInflationContributionUpdate, etc.)
/// which were identical boilerplate differing only in type parameter.
/// </summary>
public class SwapContributionUpdate<TContribution> : IContributionUpdate<TContribution>
    where TContribution : class
{
    public SwapContributionUpdate(TContribution update, TContribution? previous)
    {
        Update = update;
        Previous = previous;
    }

    public TContribution Update { get; }
    public TContribution? Previous { get; }
}
