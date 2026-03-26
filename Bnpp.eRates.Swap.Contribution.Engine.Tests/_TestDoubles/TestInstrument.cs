using Bnpp.eRates.Contribution.Library.Common;
using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.SmartBase.AppBase.Contracts;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;

/// <summary>
/// Minimal ISwapInstrument implementation for unit tests.
/// Has both a parameterless ctor (for generic constraints) and a
/// dictionary ctor (for the compiled expression factory tests).
/// </summary>
public class TestInstrument : ISwapInstrument, IKey<string>, IIsDeleted
{
    public TestInstrument() { }

    public TestInstrument(IDictionary<string, object> items)
    {
        Id = items.TryGetValue("Id", out var id) ? id?.ToString() ?? "" : "";
        Description = items.TryGetValue("Description", out var desc) ? desc?.ToString() ?? "" : "";
        Currency = items.TryGetValue("Currency", out var ccy) ? ccy?.ToString() ?? "" : "";
        MarketId = items.TryGetValue("MarketId", out var mid) ? mid?.ToString() ?? "" : "";
        Key = Id;
    }

    public string Id { get; set; } = string.Empty;
    public string MarketId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DateMaturity { get; set; }
    public string Currency { get; set; } = string.Empty;
    public InstrumentKey InstrumentKey => new(Id);
    public string Key { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Instrument type deliberately missing the dictionary constructor.
/// Used to verify the compiled factory throws at startup.
/// </summary>
public class InstrumentWithoutDictionaryCtor : ISwapInstrument, IKey<string>, IIsDeleted
{
    public string Id { get; set; } = string.Empty;
    public string MarketId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DateMaturity { get; set; }
    public string Currency { get; set; } = string.Empty;
    public InstrumentKey InstrumentKey => new(Id);
    public string Key { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
