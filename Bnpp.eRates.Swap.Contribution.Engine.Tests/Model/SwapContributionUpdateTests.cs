using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Model;

public class SwapContributionUpdateTests
{
    [Fact]
    public void Constructor_StoresUpdateAndPrevious()
    {
        var current = new TestContribution { Key = "K1", InstrumentId = "INS1" };
        var previous = new TestContribution { Key = "K1", InstrumentId = "INS1" };

        var update = new SwapContributionUpdate<TestContribution>(current, previous);

        Assert.Same(current, update.Update);
        Assert.Same(previous, update.Previous);
    }

    [Fact]
    public void Constructor_AllowsNullPrevious()
    {
        var current = new TestContribution { Key = "K1" };

        var update = new SwapContributionUpdate<TestContribution>(current, null);

        Assert.Same(current, update.Update);
        Assert.Null(update.Previous);
    }

    [Fact]
    public void Update_IsNeverNull()
    {
        var current = new TestContribution { Key = "K1" };
        var update = new SwapContributionUpdate<TestContribution>(current, null);

        Assert.NotNull(update.Update);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Verify the properties don't have setters (compile-time guarantee via API shape)
        var type = typeof(SwapContributionUpdate<TestContribution>);

        var updateProp = type.GetProperty(nameof(SwapContributionUpdate<TestContribution>.Update))!;
        var previousProp = type.GetProperty(nameof(SwapContributionUpdate<TestContribution>.Previous))!;

        Assert.False(updateProp.CanWrite, "Update property should be read-only");
        Assert.False(previousProp.CanWrite, "Previous property should be read-only");
    }

    [Fact]
    public void GenericType_WorksWithDifferentContributionTypes()
    {
        // Verify the generic wrapper works with any class type
        var contribution = new TestContribution { Key = "X", Market = "EBM" };
        var update = new SwapContributionUpdate<TestContribution>(contribution, null);

        Assert.Equal("X", update.Update.Key);
        Assert.Equal("EBM", update.Update.Market);
    }
}
