using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;
using Microsoft.AspNetCore.SignalR;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

/// <summary>
/// Tests that the generic Hub correctly gates write operations based on the
/// product's configured capabilities. This is the core mechanism that allows
/// different products to expose different subsets of hub methods.
///
/// These tests use null for the subscription manager because capability checks
/// happen BEFORE any delegation — failures short-circuit cleanly.
/// </summary>
public class SwapContributionHubCapabilityTests
{
    private readonly HubTestFixture _fixture;

    public SwapContributionHubCapabilityTests()
    {
        // Start with NO capabilities — all write ops should fail
        _fixture = new HubTestFixture(ProductDefinitionFactory.CreateWithNoCapabilities());
    }

    // ── Capability rejection tests ──
    // Each test verifies that calling a hub method without the required capability throws HubException

    [Fact]
    public async Task QuoteOn_ThrowsHubException_WhenQuoteOnOffCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.Contains(SwapContributionCapability.QuoteOnOff, ex.Message);
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuoteOff_ThrowsHubException_WhenQuoteOnOffCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.QuoteOff("corr-1", new[] { "key1" }));

        Assert.Contains(SwapContributionCapability.QuoteOnOff, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteAskMidSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteAskMidSpread("corr-1", new[] { "id1" }, 0.5));

        Assert.Contains(SwapContributionCapability.AdjustAskMidSpread, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteBidMidSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteBidMidSpread("corr-1", new[] { "id1" }, 0.5));

        Assert.Contains(SwapContributionCapability.AdjustBidMidSpread, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteAskQuantity_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteAskQuantity("corr-1", new[] { "id1" }, 100));

        Assert.Contains(SwapContributionCapability.AdjustAskQuantity, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteBidQuantity_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteBidQuantity("corr-1", new[] { "id1" }, 100));

        Assert.Contains(SwapContributionCapability.AdjustBidQuantity, ex.Message);
    }

    [Fact]
    public async Task AdjustVwapAskSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustVwapAskSpread("corr-1", new[] { "id1" }, 0.1));

        Assert.Contains(SwapContributionCapability.AdjustVwapAskSpread, ex.Message);
    }

    [Fact]
    public async Task AdjustVwapBidSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustVwapBidSpread("corr-1", new[] { "id1" }, 0.1));

        Assert.Contains(SwapContributionCapability.AdjustVwapBidSpread, ex.Message);
    }

    [Fact]
    public async Task AdjustVwapAskSize_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustVwapAskSize("corr-1", new[] { "id1" }, 1000));

        Assert.Contains(SwapContributionCapability.AdjustVwapAskSize, ex.Message);
    }

    [Fact]
    public async Task AdjustVwapBidSize_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustVwapBidSize("corr-1", new[] { "id1" }, 1000));

        Assert.Contains(SwapContributionCapability.AdjustVwapBidSize, ex.Message);
    }

    [Fact]
    public async Task SwitchQuotingSource_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.SwitchQuotingSource("corr-1", new[] { "id1" }, "BBG"));

        Assert.Contains(SwapContributionCapability.SwitchQuotingSource, ex.Message);
    }

    [Fact]
    public async Task SwitchMidSource_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.SwitchMidSource("corr-1", new[] { "id1" }, "TWEB"));

        Assert.Contains(SwapContributionCapability.SwitchMidSource, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteMidPrice_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteMidPrice("corr-1", new[] { "id1" }, 99.5));

        Assert.Contains(SwapContributionCapability.AdjustMidPrice, ex.Message);
    }

    [Fact]
    public async Task SetMidYield_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.SetMidYield("corr-1", new[] { "id1" }, 1.25));

        Assert.Contains(SwapContributionCapability.SetMidYield, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteAskMidYieldSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteAskMidYieldSpread("corr-1", new[] { "id1" }, 0.02));

        Assert.Contains(SwapContributionCapability.AdjustAskMidYieldSpread, ex.Message);
    }

    [Fact]
    public async Task AdjustQuoteBidMidYieldSpread_ThrowsHubException_WhenCapabilityMissing()
    {
        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.AdjustQuoteBidMidYieldSpread("corr-1", new[] { "id1" }, 0.02));

        Assert.Contains(SwapContributionCapability.AdjustBidMidYieldSpread, ex.Message);
    }

    // ── Capability inclusion tests ──
    // Verify that methods pass the capability check when the capability IS configured.
    // These proceed past the capability check but hit the subscription manager (null),
    // proving the gating logic passed.

    [Fact]
    public async Task QuoteOn_PassesCapabilityCheck_WhenCapabilityConfigured()
    {
        _fixture.Product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.QuoteOnOff);
        _fixture.RebuildHub();

        // Should NOT throw HubException for capability — will throw NullRef
        // from subscription manager call (which proves we passed the gate)
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => _fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.IsNotType<HubException>(ex);
    }

    [Fact]
    public async Task AdjustVwapAskSpread_PassesCapabilityCheck_WhenCapabilityConfigured()
    {
        _fixture.Product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.AdjustVwapAskSpread);
        _fixture.RebuildHub();

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => _fixture.Hub.AdjustVwapAskSpread("corr-1", new[] { "id1" }, 0.5));

        Assert.IsNotType<HubException>(ex);
    }

    // ── Error message quality ──

    [Fact]
    public async Task CapabilityError_IncludesProductId()
    {
        _fixture.Product = ProductDefinitionFactory.CreateWithNoCapabilities();
        _fixture.Product.Id = "SwapLatam";
        _fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => _fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.Contains("SwapLatam", ex.Message);
    }
}
