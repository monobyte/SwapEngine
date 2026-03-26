using System.Security.Claims;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests._Helpers;

/// <summary>
/// Provides a pre-configured Hub instance for unit tests.
///
/// The Hub's subscription manager is set to null. This is safe for testing:
///   - Authorization/capability FAILURE paths → short-circuit before reaching the sub manager
///   - Authorization SUCCESS paths → we verify the auth service was called correctly
///
/// For full end-to-end hub tests (including subscription manager calls), extract an
/// interface from the subscription manager and mock it. See ARCHITECTURE_NOTES.md.
/// </summary>
internal class HubTestFixture
{
    public Mock<ILogger<SwapContributionHub<TestContribution, TestInstrument, TestTiers>>> MockLogger { get; }
    public Mock<IAuthorizationService> MockAuthService { get; }
    public Mock<HubCallerContext> MockHubContext { get; }
    public SwapProductDefinition Product { get; set; }

    public SwapContributionHub<TestContribution, TestInstrument, TestTiers> Hub { get; private set; } = null!;

    public const string TestConnectionId = "test-connection-001";
    public const string TestUserName = "DOMAIN\\testuser";

    public HubTestFixture(SwapProductDefinition? product = null)
    {
        MockLogger = new Mock<ILogger<SwapContributionHub<TestContribution, TestInstrument, TestTiers>>>();
        MockAuthService = new Mock<IAuthorizationService>();
        MockHubContext = new Mock<HubCallerContext>();
        Product = product ?? ProductDefinitionFactory.CreateValid();

        SetupAuthenticatedUser(TestUserName);
        SetupAuthorizationSucceeds();
        RebuildHub();
    }

    /// <summary>Rebuild the Hub instance (call after changing Product or auth setup).</summary>
    public void RebuildHub()
    {
        // The subscription manager is null — acceptable for auth/capability failure-path tests.
        // Write-success paths will NullRef, which is expected and documented.
        Hub = new SwapContributionHub<TestContribution, TestInstrument, TestTiers>(
            MockLogger.Object,
            MockAuthService.Object,
            null!,  // subscription manager — see class doc
            Product);

        Hub.Context = MockHubContext.Object;
    }

    // ── User identity setup ──

    public void SetupAuthenticatedUser(string userName)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userName)
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        MockHubContext.Setup(c => c.ConnectionId).Returns(TestConnectionId);
        MockHubContext.Setup(c => c.User).Returns(principal);
    }

    public void SetupAnonymousUser()
    {
        var identity = new ClaimsIdentity(); // No auth type = unauthenticated
        var principal = new ClaimsPrincipal(identity);

        MockHubContext.Setup(c => c.ConnectionId).Returns(TestConnectionId);
        MockHubContext.Setup(c => c.User).Returns(principal);
    }

    public void SetupNullUserName()
    {
        // Identity exists but Name is null
        var identity = new ClaimsIdentity("TestAuth");
        var principal = new ClaimsPrincipal(identity);

        MockHubContext.Setup(c => c.ConnectionId).Returns(TestConnectionId);
        MockHubContext.Setup(c => c.User).Returns(principal);
    }

    // ── Authorization setup ──

    public void SetupAuthorizationSucceeds()
    {
        MockAuthService
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
    }

    public void SetupAuthorizationFails(string? policyName = null)
    {
        var setup = policyName != null
            ? MockAuthService.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                policyName))
            : MockAuthService.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()));

        setup.ReturnsAsync(AuthorizationResult.Failed());
    }
}
