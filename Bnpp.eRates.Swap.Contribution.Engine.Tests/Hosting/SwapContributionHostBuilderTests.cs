using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

/// <summary>
/// Tests for the SwapContributionHostBuilder static factory and
/// SwapContributionApp fluent API (ConfigureServices, ConfigureApp).
/// </summary>
public class SwapContributionHostBuilderTests
{
    [Fact]
    public void Create_ReturnsNonNullApp()
    {
        var app = SwapContributionHostBuilder
            .Create<TestContribution, TestInstrument, TestTiers>(Array.Empty<string>());

        Assert.NotNull(app);
    }

    [Fact]
    public void ConfigureServices_ReturnsSameInstance_ForFluency()
    {
        var app = SwapContributionHostBuilder
            .Create<TestContribution, TestInstrument, TestTiers>(Array.Empty<string>());

        var result = app.ConfigureServices(services => { });

        Assert.Same(app, result);
    }

    [Fact]
    public void ConfigureApp_ReturnsSameInstance_ForFluency()
    {
        var app = SwapContributionHostBuilder
            .Create<TestContribution, TestInstrument, TestTiers>(Array.Empty<string>());

        var result = app.ConfigureApp(app => { });

        Assert.Same(app, result);
    }

    [Fact]
    public void FluentApi_AllowsChaining()
    {
        // Verify the complete fluent chain compiles and works
        var app = SwapContributionHostBuilder
            .Create<TestContribution, TestInstrument, TestTiers>(Array.Empty<string>())
            .ConfigureServices(services => { /* custom service */ })
            .ConfigureApp(app => { /* custom middleware */ });

        Assert.NotNull(app);
    }

    [Fact]
    public void Create_AcceptsCommandLineArgs()
    {
        var args = new[] { "--environment", "Development", "--instance", "01" };

        var app = SwapContributionHostBuilder
            .Create<TestContribution, TestInstrument, TestTiers>(args);

        Assert.NotNull(app);
    }
}
