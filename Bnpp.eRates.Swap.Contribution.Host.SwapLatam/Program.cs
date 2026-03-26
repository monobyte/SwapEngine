using Bnpp.eRates.Contribution.Model.SwapLatam;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Products.SwapLatam;

// That's it. The engine reads Product config from appsettings.json,
// wires up all services, configures the pipeline, maps the hub, and runs.
SwapContributionHostBuilder
    .Create<SwapLatamContribution, SwapLatamInstrument, SwapTiers>(args)
    .Run();
