using Bnpp.eRates.Contribution.Library.Common;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.SmartBase.AppBase.Contracts;
using Bnpp.SmartBase.Orion.Orion;

namespace Bnpp.eRates.Swap.Contribution.Products.SwapLatam;

/// <summary>
/// SwapLatam instrument model. Just the properties and Orion field mapping.
/// </summary>
public class SwapLatamInstrument : ISwapInstrument, IIsDeleted, IKey<string>
{
    // Parameterless ctor for deserialization
    public SwapLatamInstrument() { }

    public SwapLatamInstrument(IDictionary<string, object> i)
    {
        Id = (i.AsLong("IID") ?? 0).ToString();
        CID = i.AsLong("CID") ?? 0;
        MarketId = (i.AsInt("MarketId") ?? 0).ToString();
        RefSource = i.AsString("RefSource");
        CapturedUtc = i.AsString("CapturedUtc");
        Description = i.AsString("Description");
        ContractMaturity = i.AsString("ContractMaturity");
        CreateDate = i.AsInt("CreateDate") ?? 0;
        MarketInstrumentId = i.AsString("MarketInstrumentId");
        Segment = i.AsString("Segment");
        ValueTick = i.AsDecimal("ValueTick") ?? 0;
        QtyMin = i.AsDecimal("QtyMin") ?? 0;
        QtyMax = i.AsDecimal("QtyMax") ?? 0;
        QtyTick = i.AsDecimal("QtyTick") ?? 0;
        Currency = i.AsString("Currency");
        ValueType = i.AsString("ValueType");
        DateMaturity = i.AsInt("DateMaturity") ?? 0;
        QtyNominal = i.AsInt("QtyNominal") ?? 0;

        if (string.IsNullOrEmpty(Segment))
        {
            Segment = Description.Contains("IMM") ? "IMM" : "STD";
        }

        Key = $"{Id}";
        IsDeleted = (i.AsInt("IsDeleted") ?? 0) != 0;
    }

    // IInstrument
    public InstrumentKey InstrumentKey => new(Id);
    public string Id { get; set; } = string.Empty;
    public string MarketId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DateMaturity { get; set; }
    public string Currency { get; set; } = string.Empty;

    // IKey / IIsDeleted
    public string Key { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }

    // Product-specific properties
    public long CID { get; set; }
    public string RefSource { get; set; } = string.Empty;
    public string CapturedUtc { get; set; } = string.Empty;
    public string ContractMaturity { get; set; } = string.Empty;
    public int CreateDate { get; set; }
    public string MarketInstrumentId { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public decimal ValueTick { get; set; }
    public decimal QtyMin { get; set; }
    public decimal QtyMax { get; set; }
    public decimal QtyTick { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public int QtyNominal { get; set; }
}
