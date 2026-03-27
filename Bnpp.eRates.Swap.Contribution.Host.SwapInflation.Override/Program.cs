using Bnpp.eRates.Contribution.Model.SwapInflation;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Products.SwapInflation;

// ──────────────────────────────────────────────────────────────────────
// EXAMPLE: Product host with custom overrides
// This shows the escape hatches for products that need non-standard behaviour.
// Most products will NOT need this — just the 5-line version.
// ──────────────────────────────────────────────────────────────────────

SwapContributionHostBuilder
    .Create<SwapInflationContribution, SwapInflationInstrument, EmeaSwapTiers>(args)
    .ConfigureNinject(kernel =>
    {
        // Example: load an additional SmartBase Ninject module
        // kernel.Load(new SomeCustomSmartBaseModule());

        // Example: override a default Ninject binding
        // kernel.Rebind<ICustomService>().To<SpecialImplementation>().InSingletonScope();
    })
    .ConfigureServices(services =>
    {
        // Example: replace a default MS DI service with a custom one
        // services.AddSingleton<IContributionProvider<...>, MyCustomProvider>();

        // Example: add a product-specific service
        // services.AddSingleton<IInflationSpecificService, InflationSpecificService>();
    })
    .ConfigureApp(app =>
    {
        // Example: add a custom REST endpoint
        // app.MapGet("/api/inflation/custom", () => "custom data");
    })
    .Run();
