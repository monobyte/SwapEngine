using System.Security.Claims;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

/// <summary>
/// Tests the Hub's dynamic authorization behaviour.
/// Policies are resolved from product config at runtime (not static [Authorize] attributes).
/// </summary>
public class SwapContributionHubAuthorizationTests
{
    // ── Write authorization failures ──

    [Fact]
    public async Task WriteOperation_ThrowsHubException_WhenWriteAuthorizationFails()
    {
        var fixture = new HubTestFixture(
            ProductDefinitionFactory.CreateWithCapabilities(SwapContributionCapability.QuoteOnOff));
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteOperation_ThrowsHubException_WhenWritePolicySpecificallyFails()
    {
        var product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.AdjustAskMidSpread);
        product.Policies.Write = "swap_latam_write";

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails("swap_latam_write");
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.AdjustQuoteAskMidSpread("corr-1", new[] { "id1" }, 0.5));

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteOperation_SkipsAuthCheck_WhenWritePolicyEmpty()
    {
        var product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.QuoteOnOff);
        product.Policies.Write = string.Empty;

        var fixture = new HubTestFixture(product);
        fixture.RebuildHub();

        // Should pass auth (no policy to check) then fail on subscription manager (NullRef).
        // The NullRef proves auth was bypassed successfully.
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.IsNotType<HubException>(ex);
        fixture.MockAuthService.Verify(
            a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task WriteOperation_ChecksCorrectPolicy()
    {
        var product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.QuoteOnOff);
        product.Policies.Write = "CustomWritePolicy";

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationSucceeds();
        fixture.RebuildHub();

        // Will pass auth then NullRef on subscription manager
        await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        fixture.MockAuthService.Verify(
            a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                "CustomWritePolicy"),
            Times.Once);
    }

    // ── Read authorization failures ──

    [Fact]
    public async Task ReadOperation_ThrowsHubException_WhenReadAuthorizationFails()
    {
        var fixture = new HubTestFixture();
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.GetAllInstruments());

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("read", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadOperation_SkipsAuthCheck_WhenReadPolicyEmpty()
    {
        var product = ProductDefinitionFactory.CreateValid();
        product.Policies.Read = string.Empty;

        var fixture = new HubTestFixture(product);
        fixture.RebuildHub();

        // Should pass auth (no policy) then fail on subscription manager (NullRef)
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Hub.GetAllInstruments());

        Assert.IsNotType<HubException>(ex);
    }

    [Fact]
    public async Task SubscribeInstruments_ChecksReadPolicy()
    {
        var product = ProductDefinitionFactory.CreateValid();
        product.Policies.Read = "CustomReadPolicy";

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails("CustomReadPolicy");
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.SubscribeInstruments(new[] { "ins1" }));

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnSubscribeInstruments_ChecksReadPolicy()
    {
        var product = ProductDefinitionFactory.CreateValid();
        product.Policies.Read = "ReadPolicy";

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails("ReadPolicy");
        fixture.RebuildHub();

        await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.UnSubscribeInstruments(new[] { "ins1" }));
    }

    [Fact]
    public async Task SubscribeInstrumentTiers_ChecksReadPolicy()
    {
        var fixture = new HubTestFixture();
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.SubscribeInstrumentTiers(new[] { "tier1" }));
    }

    [Fact]
    public async Task SetClientDefaultTier_ChecksReadPolicy()
    {
        var fixture = new HubTestFixture();
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.SetClientDefaultTier("T1"));
    }

    // ── User identity validation ──

    [Fact]
    public async Task WriteOperation_ThrowsHubException_WhenUserNameIsNull()
    {
        var fixture = new HubTestFixture(
            ProductDefinitionFactory.CreateWithCapabilities(SwapContributionCapability.QuoteOnOff));
        fixture.Product.Policies.Write = string.Empty; // Skip auth to reach user check
        fixture.SetupNullUserName();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.Contains("User identity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Ordering: capability checked before auth ──

    [Fact]
    public async Task CapabilityCheck_HappensBeforeAuthCheck()
    {
        // Product has NO capabilities, but auth would also fail
        var product = ProductDefinitionFactory.CreateWithNoCapabilities();
        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        // Should fail on capability, NOT on auth
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
        // Auth service should NOT have been called
        fixture.MockAuthService.Verify(
            a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Error message quality ──

    [Fact]
    public async Task AuthorizationError_IncludesProductId()
    {
        var product = ProductDefinitionFactory.CreateWithCapabilities(
            SwapContributionCapability.QuoteOnOff);
        product.Id = "SwapInflation";

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.QuoteOn("corr-1", new[] { "key1" }));

        Assert.Contains("SwapInflation", ex.Message);
    }
}
