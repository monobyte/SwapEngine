using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Configuration;

/// <summary>
/// Verifies that SwapProductDefinition correctly binds from IConfiguration (JSON).
/// This is critical because all per-product config comes from appsettings.json.
/// </summary>
public class SwapProductDefinitionBindingTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void Binds_TopLevelProperties_FromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "SwapLatam",
            ["Product:InstanceEnvVar"] = "ERATESWEB_LDN_DEV_HOST_SWAP_LATAM",
            ["Product:Endpoint"] = "SwapLatam",
            ["Product:EndpointType"] = "SwapLatamContribution",
            ["Product:DefaultTier"] = "T1",
            ["Product:ThrottleDelay"] = "2.5",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal("SwapLatam", product!.Id);
        Assert.Equal("ERATESWEB_LDN_DEV_HOST_SWAP_LATAM", product.InstanceEnvVar);
        Assert.Equal("SwapLatam", product.Endpoint);
        Assert.Equal("SwapLatamContribution", product.EndpointType);
        Assert.Equal("T1", product.DefaultTier);
        Assert.Equal(2.5, product.ThrottleDelay);
    }

    [Fact]
    public void Binds_CapabilitiesArray_FromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "Test",
            ["Product:Capabilities:0"] = "QuoteOnOff",
            ["Product:Capabilities:1"] = "AdjustAskMidSpread",
            ["Product:Capabilities:2"] = "SwitchQuotingSource",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal(3, product!.Capabilities.Count);
        Assert.Contains("QuoteOnOff", product.Capabilities);
        Assert.Contains("AdjustAskMidSpread", product.Capabilities);
        Assert.Contains("SwitchQuotingSource", product.Capabilities);
    }

    [Fact]
    public void Binds_NestedFeedConfiguration_FromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "Test",
            ["Product:Feeds:Contributions:0:ConnectionKey"] = "Feed1",
            ["Product:Feeds:Contributions:1:ConnectionKey"] = "Feed2",
            ["Product:Feeds:Contributions:2:ConnectionKey"] = "Feed3",
            ["Product:Feeds:Instruments:ConnectionKey"] = "InstrFeed",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal(3, product!.Feeds.Contributions.Count);
        Assert.Equal("Feed1", product.Feeds.Contributions[0].ConnectionKey);
        Assert.Equal("Feed2", product.Feeds.Contributions[1].ConnectionKey);
        Assert.Equal("Feed3", product.Feeds.Contributions[2].ConnectionKey);
        Assert.Equal("InstrFeed", product.Feeds.Instruments.ConnectionKey);
    }

    [Fact]
    public void Binds_Policies_FromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "Test",
            ["Product:Policies:Read"] = "SwapLatamRead",
            ["Product:Policies:Write"] = "SwapLatamWrite",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal("SwapLatamRead", product!.Policies.Read);
        Assert.Equal("SwapLatamWrite", product.Policies.Write);
    }

    [Fact]
    public void Binds_SignalRConfig_FromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "Test",
            ["Product:SignalR:EventContributionUpdate"] = "customUpdateEvent",
            ["Product:SignalR:EventNotification"] = "customNotifyEvent",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal("customUpdateEvent", product!.SignalR.EventContributionUpdate);
        Assert.Equal("customNotifyEvent", product.SignalR.EventNotification);
    }

    [Fact]
    public void Binds_SingleFeed_MatchesSwapLatamConfig()
    {
        // Mirror the actual SwapLatam appsettings.json structure
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "SwapLatam",
            ["Product:Feeds:Contributions:0:ConnectionKey"] = "SwapLatamContributions",
            ["Product:Feeds:Instruments:ConnectionKey"] = "SwapLatamInstruments",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Single(product!.Feeds.Contributions);
        Assert.Equal("SwapLatamContributions", product.Feeds.Contributions[0].ConnectionKey);
    }

    [Fact]
    public void Binds_MultipleFeeds_MatchesSwapInflationConfig()
    {
        // Mirror the actual SwapInflation appsettings.json structure (4 feeds)
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Product:Id"] = "SwapInflation",
            ["Product:Feeds:Contributions:0:ConnectionKey"] = "SwapInflationContributionsTweb",
            ["Product:Feeds:Contributions:1:ConnectionKey"] = "SwapInflationContributionsTwebGbp",
            ["Product:Feeds:Contributions:2:ConnectionKey"] = "SwapInflationContributionsBbgBpgb",
            ["Product:Feeds:Contributions:3:ConnectionKey"] = "SwapInflationContributionsBbgBpsx",
            ["Product:Feeds:Instruments:ConnectionKey"] = "SwapInflationInstruments",
        });

        var product = config.GetSection(SwapProductDefinition.SectionName)
            .Get<SwapProductDefinition>();

        Assert.NotNull(product);
        Assert.Equal(4, product!.Feeds.Contributions.Count);
    }
}
