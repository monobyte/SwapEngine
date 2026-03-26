using Bnpp.eRates.Swap.Contribution.Engine.Configuration;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;

/// <summary>
/// Builder for creating SwapProductDefinition instances in tests.
/// Defaults to a valid, fully-configured product to reduce test boilerplate.
/// </summary>
internal static class ProductDefinitionFactory
{
    /// <summary>
    /// Creates a valid product definition with all fields populated.
    /// Tests can then override individual properties as needed.
    /// </summary>
    public static SwapProductDefinition CreateValid(Action<SwapProductDefinition>? configure = null)
    {
        var product = new SwapProductDefinition
        {
            Id = "TestSwap",
            InstanceEnvVar = "ERATESWEB_LDN_DEV_HOST_TEST_SWAP",
            Endpoint = "TestSwap",
            EndpointType = "TestSwapContribution",
            DefaultTier = "T1",
            ThrottleDelay = 1.0,
            Policies = new SwapPolicyConfig
            {
                Read = "TestSwapRead",
                Write = "TestSwapWrite"
            },
            Capabilities = new List<string>
            {
                SwapContributionCapability.QuoteOnOff,
                SwapContributionCapability.AdjustAskMidSpread,
                SwapContributionCapability.AdjustBidMidSpread,
                SwapContributionCapability.AdjustAskQuantity,
                SwapContributionCapability.AdjustBidQuantity,
                SwapContributionCapability.AdjustVwapAskSpread,
                SwapContributionCapability.AdjustVwapBidSpread,
                SwapContributionCapability.AdjustVwapAskSize,
                SwapContributionCapability.AdjustVwapBidSize,
                SwapContributionCapability.SwitchQuotingSource,
            },
            Feeds = new SwapFeedConfig
            {
                Contributions = new List<SwapFeedConnectionConfig>
                {
                    new() { ConnectionKey = "TestSwapContributions" }
                },
                Instruments = new SwapInstrumentConnectionConfig
                {
                    ConnectionKey = "TestSwapInstruments"
                }
            },
            SignalR = new SwapSignalRConfig
            {
                EventContributionUpdate = "onContributionUpdate",
                EventNotification = "onNotification"
            }
        };

        configure?.Invoke(product);
        return product;
    }

    /// <summary>Creates a product with NO capabilities — all write operations will be rejected.</summary>
    public static SwapProductDefinition CreateWithNoCapabilities() =>
        CreateValid(p => p.Capabilities = new List<string>());

    /// <summary>Creates a product with a specific subset of capabilities.</summary>
    public static SwapProductDefinition CreateWithCapabilities(params string[] capabilities) =>
        CreateValid(p => p.Capabilities = capabilities.ToList());

    /// <summary>Creates a product with no authorization policies (auth checks are skipped).</summary>
    public static SwapProductDefinition CreateWithNoPolicies() =>
        CreateValid(p =>
        {
            p.Policies.Read = string.Empty;
            p.Policies.Write = string.Empty;
        });

    /// <summary>Creates a product with multiple contribution feeds (like SwapInflation).</summary>
    public static SwapProductDefinition CreateWithMultipleFeeds(int feedCount) =>
        CreateValid(p =>
        {
            p.Feeds.Contributions = Enumerable.Range(1, feedCount)
                .Select(i => new SwapFeedConnectionConfig { ConnectionKey = $"Feed{i}" })
                .ToList();
        });

    /// <summary>Creates a minimal valid product definition (for validation boundary tests).</summary>
    public static SwapProductDefinition CreateMinimal() => new()
    {
        Id = "Min",
        InstanceEnvVar = "ENV",
        Endpoint = "Ep",
        Feeds = new SwapFeedConfig
        {
            Contributions = new List<SwapFeedConnectionConfig>
            {
                new() { ConnectionKey = "C1" }
            },
            Instruments = new SwapInstrumentConnectionConfig { ConnectionKey = "I1" }
        }
    };
}
