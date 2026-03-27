using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Configuration;
using Bnpp.eRates.Swap.Contribution.Engine.Feeds;
using Bnpp.eRates.Swap.Contribution.Engine.Model;
using Bnpp.eRates.Swap.Contribution.Engine.Providers;
using Bnpp.eRates.Web.Helper;
using Ninject;
using Ninject.Modules;
using Ninject.Parameters;

namespace Bnpp.eRates.Swap.Contribution.Engine.Ninject;

/// <summary>
/// Generic Ninject module that replaces ALL per-product Library Ioc modules
/// (Library.SwapLatam.Ioc, Library.SwapInflation.Ioc, etc.)
///
/// The old per-product modules were identical in structure — they just varied in:
///   - Type parameters (SwapLatamInstrument vs SwapInflationInstrument, etc.)
///   - Connection constant strings (now from product config)
///   - Number of feeds (1 for Latam, 4 for Inflation — now config-driven)
///
/// SmartBase dependencies (WireClientFactory, OrionSubscriptionFactory, IDispatcher,
/// IOrionClientFactory, IOrionFunctionCall, etc.) are resolved by Ninject from its
/// module chain. We provide the config-driven parameters via ConstructorArgument.
/// </summary>
internal class SwapContributionNinjectModule<TContribution, TInstrument, TTiers> : NinjectModule
    where TContribution : class, ISwapContribution<TInstrument, TTiers>, IKey<string>, IIsDeleted
    where TInstrument : class, ISwapInstrument, IKey<string>, IIsDeleted
    where TTiers : struct
{
    private readonly SwapProductDefinition _product;
    private readonly AppConfig _appConfig;

    public SwapContributionNinjectModule(SwapProductDefinition product, AppConfig appConfig)
    {
        _product = product;
        _appConfig = appConfig;
    }

    public override void Load()
    {
        // ── 1. Instrument feed ──
        // Ninject resolves SmartBase deps (WireClientFactory, OrionFeedMonitorFeed,
        // OrionSubscriptionFactory). We provide the config-driven parameters.
        var instrumentConfig = _appConfig.GetConnection(_product.Feeds.Instruments.ConnectionKey);
        var instrumentFeed = Kernel.Get<GenericSwapInstrumentFeed<TInstrument>>(
            new ConstructorArgument("config", instrumentConfig),
            new ConstructorArgument("productId", _product.Id));

        Bind<ISwapInstrumentFeed<TInstrument>>()
            .ToConstant(instrumentFeed)
            .InSingletonScope();

        // ── 2. Instrument provider ──
        // IDispatcher + ISwapInstrumentFeed<T> resolved from kernel
        var instrumentProvider = Kernel.Get<GenericSwapInstrumentProvider<TInstrument>>();

        Bind<IInstrumentProvider<TInstrument>>()
            .ToConstant(instrumentProvider)
            .InSingletonScope();

        // ── 3. Contribution feeds (config-driven) ──
        // One feed per entry in product config. Latam has 1, Inflation has 4.
        // Ninject resolves: WireClientFactory, OrionFeedMonitorFeed,
        //   OrionSubscriptionFactory, IOrionClientFactory, IOrionFunctionCall.
        // We provide: orionConfig (per feed), instrumentProvider.
        var feeds = _product.Feeds.Contributions.Select(feedConfig =>
        {
            var orionConfig = _appConfig.GetConnection(feedConfig.ConnectionKey);
            return (IContributionFeed<TInstrument, TTiers>)
                Kernel.Get<GenericSwapContributionFeed<TContribution, TInstrument, TTiers>>(
                    new ConstructorArgument("orionConfig", orionConfig),
                    new ConstructorArgument("instrumentProvider", instrumentProvider));
        }).ToArray();

        // ── 4. Contribution provider ──
        // IDispatcher resolved from kernel. Feeds array passed explicitly.
        var contributionProvider =
            Kernel.Get<GenericSwapContributionProvider<TContribution, TInstrument, TTiers>>(
                new ConstructorArgument("feeds", feeds));

        Bind<IContributionProvider<SwapContributionUpdate<TContribution>, TContribution>>()
            .ToConstant(contributionProvider)
            .InSingletonScope();
    }
}
