namespace Bnpp.eRates.Swap.Contribution.Engine.Configuration;

/// <summary>
/// Known capability identifiers. A product's "Capabilities" config list references these
/// to control which hub methods are active. Methods for unlisted capabilities will return
/// a "not supported" error if called.
/// </summary>
public static class SwapContributionCapability
{
    // Quote on/off
    public const string QuoteOnOff = "QuoteOnOff";

    // Spread and quantity adjustments
    public const string AdjustAskMidSpread = "AdjustAskMidSpread";
    public const string AdjustBidMidSpread = "AdjustBidMidSpread";
    public const string AdjustAskQuantity = "AdjustAskQuantity";
    public const string AdjustBidQuantity = "AdjustBidQuantity";

    // VWAP adjustments
    public const string AdjustVwapAskSpread = "AdjustVwapAskSpread";
    public const string AdjustVwapBidSpread = "AdjustVwapBidSpread";
    public const string AdjustVwapAskSize = "AdjustVwapAskSize";
    public const string AdjustVwapBidSize = "AdjustVwapBidSize";

    // Source switching
    public const string SwitchQuotingSource = "SwitchQuotingSource";
    public const string SwitchMidSource = "SwitchMidSource";

    // Mid price / yield
    public const string AdjustMidPrice = "AdjustMidPrice";
    public const string SetMidYield = "SetMidYield";
    public const string AdjustAskMidYieldSpread = "AdjustAskMidYieldSpread";
    public const string AdjustBidMidYieldSpread = "AdjustBidMidYieldSpread";
}
