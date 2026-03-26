using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.SmartBase.AppBase.Contracts;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;

/// <summary>
/// Minimal ISwapContribution implementation for unit tests.
/// Has both a parameterless ctor and an (orionKey, dictionary) ctor
/// matching the contract required by the compiled expression factory.
/// </summary>
public class TestContribution : ISwapContribution<TestInstrument, TestTiers>, IKey<string>, IIsDeleted
{
    public TestContribution() { }

    public TestContribution(string orionKey, IDictionary<string, object> items)
    {
        OrionKey = orionKey;
        InstrumentId = items.TryGetValue("InstrumentId", out var iid) ? iid?.ToString() ?? "" : "";
        Market = items.TryGetValue("Market", out var m) ? m?.ToString() ?? "" : "";
        Key = orionKey;
        UiKey = InstrumentId;
    }

    public string InstrumentId { get; set; } = string.Empty;
    public TestInstrument Instrument { get; set; } = new();
    public string UiKey { get; set; } = string.Empty;
    public string OrionKey { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public TestTiers Tier { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Contribution type deliberately missing the (string, IDictionary) constructor.
/// Used to verify the compiled factory throws at startup.
/// </summary>
public class ContributionWithoutDictionaryCtor : ISwapContribution<TestInstrument, TestTiers>, IKey<string>, IIsDeleted
{
    public string InstrumentId { get; set; } = string.Empty;
    public TestInstrument Instrument { get; set; } = new();
    public string UiKey { get; set; } = string.Empty;
    public string OrionKey { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public TestTiers Tier { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
