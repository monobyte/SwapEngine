using System.Linq.Expressions;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Feeds;

/// <summary>
/// Tests the compiled expression factory pattern used by GenericSwapContributionFeed
/// and GenericSwapInstrumentFeed to construct product types without per-call reflection.
///
/// The feeds use Expression.Lambda to compile a direct constructor call at startup.
/// These tests verify the pattern works correctly with real types and fails clearly
/// when the expected constructor is missing.
/// </summary>
public class CompiledFactoryTests
{
    // ── Instrument factory (IDictionary<string, object> ctor) ──

    [Fact]
    public void InstrumentFactory_Compiles_WhenDictionaryCtorExists()
    {
        var factory = BuildInstrumentFactory<TestInstrument>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void InstrumentFactory_CreatesInstance_WithCorrectData()
    {
        var factory = BuildInstrumentFactory<TestInstrument>();
        var data = new Dictionary<string, object>
        {
            ["Id"] = "42",
            ["Description"] = "EUR 5Y Swap",
            ["Currency"] = "EUR",
            ["MarketId"] = "EUROPE"
        };

        var instrument = factory(data);

        Assert.NotNull(instrument);
        Assert.Equal("42", instrument.Id);
        Assert.Equal("EUR 5Y Swap", instrument.Description);
        Assert.Equal("EUR", instrument.Currency);
        Assert.Equal("EUROPE", instrument.MarketId);
    }

    [Fact]
    public void InstrumentFactory_Throws_WhenDictionaryCtorMissing()
    {
        Assert.Throws<InvalidOperationException>(
            () => BuildInstrumentFactory<InstrumentWithoutDictionaryCtor>());
    }

    [Fact]
    public void InstrumentFactory_HandlesEmptyDictionary()
    {
        var factory = BuildInstrumentFactory<TestInstrument>();

        var instrument = factory(new Dictionary<string, object>());

        Assert.NotNull(instrument);
        Assert.Equal("", instrument.Id);
    }

    // ── Contribution factory (string + IDictionary<string, object> ctor) ──

    [Fact]
    public void ContributionFactory_Compiles_WhenTwoArgCtorExists()
    {
        var factory = BuildContributionFactory<TestContribution>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void ContributionFactory_CreatesInstance_WithCorrectData()
    {
        var factory = BuildContributionFactory<TestContribution>();
        var data = new Dictionary<string, object>
        {
            ["InstrumentId"] = "INS-001",
            ["Market"] = "EBM_LATAM"
        };

        var contribution = factory("orion-key-123", data);

        Assert.NotNull(contribution);
        Assert.Equal("orion-key-123", contribution.OrionKey);
        Assert.Equal("INS-001", contribution.InstrumentId);
        Assert.Equal("EBM_LATAM", contribution.Market);
    }

    [Fact]
    public void ContributionFactory_Throws_WhenTwoArgCtorMissing()
    {
        Assert.Throws<InvalidOperationException>(
            () => BuildContributionFactory<ContributionWithoutDictionaryCtor>());
    }

    [Fact]
    public void ContributionFactory_SetsKeyFromOrionKey()
    {
        var factory = BuildContributionFactory<TestContribution>();

        var contribution = factory("my-orion-key", new Dictionary<string, object>());

        Assert.Equal("my-orion-key", contribution.Key);
    }

    // ── Performance: factory is a compiled delegate, not reflection ──

    [Fact]
    public void InstrumentFactory_CanCreateManyInstancesEfficiently()
    {
        var factory = BuildInstrumentFactory<TestInstrument>();
        var data = new Dictionary<string, object> { ["Id"] = "1" };

        // Create 10,000 instances — should be fast with compiled expression
        var instruments = Enumerable.Range(0, 10_000)
            .Select(_ => factory(data))
            .ToList();

        Assert.Equal(10_000, instruments.Count);
        Assert.All(instruments, i => Assert.NotNull(i));
    }

    [Fact]
    public void ContributionFactory_CanCreateManyInstancesEfficiently()
    {
        var factory = BuildContributionFactory<TestContribution>();
        var data = new Dictionary<string, object>();

        var contributions = Enumerable.Range(0, 10_000)
            .Select(i => factory($"key-{i}", data))
            .ToList();

        Assert.Equal(10_000, contributions.Count);
    }

    // ── Helpers: replicate the factory-building logic from the feeds ──
    // These mirror GenericSwapInstrumentFeed.BuildFactory and GenericSwapContributionFeed.BuildFactory

    private static Func<IDictionary<string, object>, T> BuildInstrumentFactory<T>()
    {
        var ctor = typeof(T).GetConstructor(new[] { typeof(IDictionary<string, object>) })
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} must have a constructor accepting IDictionary<string, object>");

        var param = Expression.Parameter(typeof(IDictionary<string, object>), "items");
        var newExpr = Expression.New(ctor, param);
        return Expression.Lambda<Func<IDictionary<string, object>, T>>(newExpr, param).Compile();
    }

    private static Func<string, IDictionary<string, object>, T> BuildContributionFactory<T>()
    {
        var ctor = typeof(T).GetConstructor(
            new[] { typeof(string), typeof(IDictionary<string, object>) })
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} must have a (string, IDictionary<string, object>) constructor");

        var orionKeyParam = Expression.Parameter(typeof(string), "orionKey");
        var itemsParam = Expression.Parameter(typeof(IDictionary<string, object>), "items");
        var newExpr = Expression.New(ctor, orionKeyParam, itemsParam);
        return Expression.Lambda<Func<string, IDictionary<string, object>, T>>(
            newExpr, orionKeyParam, itemsParam).Compile();
    }
}
