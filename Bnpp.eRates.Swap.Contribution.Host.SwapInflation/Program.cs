using Bnpp.eRates.Contribution.Model.SwapInflation;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Products.SwapInflation;

SwapContributionHostBuilder
    .Create<SwapInflationContribution, SwapInflationInstrument, EmeaSwapTiers>(args)
    .Run();
