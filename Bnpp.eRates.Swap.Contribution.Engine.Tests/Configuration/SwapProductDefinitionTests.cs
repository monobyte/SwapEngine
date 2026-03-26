using Bnpp.eRates.Swap.Contribution.Engine.Configuration;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Configuration;

public class SwapProductDefinitionTests
{
    // ── HasCapability ──

    [Fact]
    public void HasCapability_ReturnsTrue_WhenCapabilityExists()
    {
        var product = new SwapProductDefinition
        {
            Capabilities = new List<string> { SwapContributionCapability.QuoteOnOff }
        };

        Assert.True(product.HasCapability(SwapContributionCapability.QuoteOnOff));
    }

    [Fact]
    public void HasCapability_ReturnsFalse_WhenCapabilityMissing()
    {
        var product = new SwapProductDefinition
        {
            Capabilities = new List<string> { SwapContributionCapability.QuoteOnOff }
        };

        Assert.False(product.HasCapability(SwapContributionCapability.AdjustMidPrice));
    }

    [Fact]
    public void HasCapability_IsCaseInsensitive()
    {
        var product = new SwapProductDefinition
        {
            Capabilities = new List<string> { "quoteonoff" }
        };

        Assert.True(product.HasCapability("QuoteOnOff"));
        Assert.True(product.HasCapability("QUOTEONOFF"));
        Assert.True(product.HasCapability("quoteonoff"));
    }

    [Fact]
    public void HasCapability_ReturnsFalse_WhenCapabilitiesEmpty()
    {
        var product = new SwapProductDefinition();

        Assert.False(product.HasCapability(SwapContributionCapability.QuoteOnOff));
    }

    // ── Defaults ──

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var product = new SwapProductDefinition();

        Assert.Equal(string.Empty, product.Id);
        Assert.Equal(string.Empty, product.InstanceEnvVar);
        Assert.Equal(string.Empty, product.Endpoint);
        Assert.Equal(string.Empty, product.EndpointType);
        Assert.Equal(string.Empty, product.DefaultTier);
        Assert.Equal(1.0, product.ThrottleDelay);
    }

    [Fact]
    public void Capabilities_DefaultsToEmptyList()
    {
        var product = new SwapProductDefinition();

        Assert.NotNull(product.Capabilities);
        Assert.Empty(product.Capabilities);
    }

    [Fact]
    public void Feeds_DefaultsToEmptyConfiguration()
    {
        var product = new SwapProductDefinition();

        Assert.NotNull(product.Feeds);
        Assert.NotNull(product.Feeds.Contributions);
        Assert.Empty(product.Feeds.Contributions);
        Assert.NotNull(product.Feeds.Instruments);
        Assert.Equal(string.Empty, product.Feeds.Instruments.ConnectionKey);
    }

    [Fact]
    public void Policies_DefaultsToEmpty()
    {
        var product = new SwapProductDefinition();

        Assert.NotNull(product.Policies);
        Assert.Equal(string.Empty, product.Policies.Read);
        Assert.Equal(string.Empty, product.Policies.Write);
    }

    [Fact]
    public void SignalR_DefaultEventNames()
    {
        var product = new SwapProductDefinition();

        Assert.Equal("onContributionUpdate", product.SignalR.EventContributionUpdate);
        Assert.Equal("onNotification", product.SignalR.EventNotification);
    }

    [Fact]
    public void SectionName_IsProduct()
    {
        Assert.Equal("Product", SwapProductDefinition.SectionName);
    }
}
