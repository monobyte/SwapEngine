using System.Reflection;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

/// <summary>
/// Tests the ValidateProductConfig method on SwapContributionApp.
/// Uses reflection to invoke the private static method directly — this is pragmatic
/// for testing validation logic that runs early in the startup pipeline.
///
/// Alternative: make ValidateProductConfig internal and use InternalsVisibleTo.
/// </summary>
public class SwapContributionAppValidationTests
{
    private static void InvokeValidation(SwapProductDefinition product)
    {
        var appType = typeof(SwapContributionApp<TestContribution, TestInstrument, TestTiers>);
        var method = appType.GetMethod("ValidateProductConfig",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ValidateProductConfig method not found");

        try
        {
            method.Invoke(null, new object[] { product });
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    [Fact]
    public void ValidProduct_DoesNotThrow()
    {
        var product = ProductDefinitionFactory.CreateMinimal();

        var ex = Record.Exception(() => InvokeValidation(product));

        Assert.Null(ex);
    }

    [Fact]
    public void ThrowsWhenIdMissing()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.Id = "";

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void ThrowsWhenInstanceEnvVarMissing()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.InstanceEnvVar = "";

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void ThrowsWhenEndpointMissing()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.Endpoint = "";

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void ThrowsWhenNoContributionFeeds()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.Feeds.Contributions.Clear();

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void ThrowsWhenInstrumentConnectionKeyMissing()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.Feeds.Instruments.ConnectionKey = "";

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void ThrowsWhenIdIsWhitespace()
    {
        var product = ProductDefinitionFactory.CreateMinimal();
        product.Id = "   ";

        Assert.ThrowsAny<ArgumentException>(() => InvokeValidation(product));
    }

    [Fact]
    public void AcceptsProductWithMultipleFeeds()
    {
        var product = ProductDefinitionFactory.CreateWithMultipleFeeds(4);

        var ex = Record.Exception(() => InvokeValidation(product));

        Assert.Null(ex);
    }
}
