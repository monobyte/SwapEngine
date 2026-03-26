using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.SmartBase.AppBase.Contracts;
using Bnpp.SmartBase.Orion.Orion;

namespace Bnpp.eRates.Swap.Contribution.Products.SwapLatam;

/// <summary>
/// SwapLatam contribution model. This is the ONLY product-specific code needed —
/// just the extra properties and their Orion field mapping.
/// The base class handles all common fields (InstrumentId, Market, Tier, Key, etc.)
/// </summary>
public class SwapLatamContribution : SwapContributionBase<SwapLatamInstrument, SwapTiers>, IIsDeleted, IKey<string>
{
    // Parameterless ctor required for deserialization and generic constraints
    public SwapLatamContribution() { }

    public SwapLatamContribution(string orionKey, IDictionary<string, object> i) : base(orionKey, i)
    {
        AskMidSpread = i.AsDecimal("AskMidSpread") ?? 0;
        BidMidSpread = i.AsDecimal("BidMidSpread") ?? 0;
        BidAskSource = i.AsString("BidAskSource");

        VwapAskSize = i.AsInt("VwapAskSize") ?? 0;
        VwapAskSpread = i.AsDecimal("VwapAskSpread") ?? 0;
        VwapBidSize = i.AsInt("VwapBidSize") ?? 0;
        VwapBidSpread = i.AsDecimal("VwapBidSpread") ?? 0;

        UiKey = InstrumentId;
    }

    // ── Product-specific properties ──
    public decimal AskMidSpread { get; set; }
    public string BidAskSource { get; set; } = string.Empty;
    public decimal BidMidSpread { get; set; }
    public int VwapAskSize { get; set; }
    public decimal VwapAskSpread { get; set; }
    public int VwapBidSize { get; set; }
    public decimal VwapBidSpread { get; set; }
}
