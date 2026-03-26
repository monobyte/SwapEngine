using Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

/// <summary>
/// Tests for GetClientDefaultTier auth behaviour and structural hub behaviour.
///
/// NOTE: OnConnectedAsync/OnDisconnectedAsync tests require a non-null subscription manager.
/// These are documented here as test stubs to be enabled once an ISubscriptionOperations
/// interface is extracted (recommended testability improvement).
/// </summary>
public class SwapContributionHubLifecycleTests
{
    [Fact]
    public async Task GetClientDefaultTier_ThrowsHubException_WhenReadAuthorizationFails()
    {
        var fixture = new HubTestFixture();
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        var ex = await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => fixture.Hub.GetClientDefaultTier());

        Assert.Contains("read", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetClientDefaultTier_SkipsAuth_WhenReadPolicyEmpty()
    {
        var product = ProductDefinitionFactory.CreateValid();
        product.Policies.Read = string.Empty;

        var fixture = new HubTestFixture(product);
        fixture.SetupAuthorizationFails();
        fixture.RebuildHub();

        await Assert.ThrowsAnyAsync<NullReferenceException>(
            () => fixture.Hub.GetClientDefaultTier());
    }

    // ── Documented stubs for tests requiring subscription manager ──
    // Enable these after extracting ISubscriptionOperations interface

    // [Fact]
    // public async Task OnConnectedAsync_SendsHostInfo()
    // {
    //     // Arrange: mock subscription manager
    //     // Act: call OnConnectedAsync
    //     // Assert: SendHostInfo called with connection id
    // }

    // [Fact]
    // public async Task OnConnectedAsync_AddsClient_WhenUserIdentified()
    // {
    //     // Assert: AddClient called with connection id and username
    // }

    // [Fact]
    // public async Task OnConnectedAsync_DoesNotAddClient_WhenUserNull()
    // {
    //     // Assert: AddClient NOT called
    // }

    // [Fact]
    // public async Task OnDisconnectedAsync_RemovesClient_WhenUserIdentified()
    // {
    //     // Assert: RemoveClient called with connection id and username
    // }

    // [Fact]
    // public async Task OnDisconnectedAsync_LogsError_WhenExceptionProvided()
    // {
    //     // Assert: logger.LogError called with exception
    // }
}
