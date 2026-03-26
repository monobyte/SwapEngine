using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Products;
using TypeGen.Core.SpecGeneration;

namespace Bnpp.eRates.Web.TypeGen;

/// <summary>
/// Refactored TypeGen spec. Auto-discovers all product types from the Products assembly
/// instead of manually listing every class per product.
///
/// When you add a new product (e.g. SwapCeemea), its Contribution and Instrument classes
/// are automatically picked up here — no manual step required.
/// </summary>
public class ERatesWebContributionGenerationSpec : ERatesWebGenerationSpecBase
{
    public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
    {
        // ── Auto-discover all swap product types ──
        var productsAssembly = typeof(Bnpp.eRates.Swap.Contribution.Products.SwapLatam.SwapLatamContribution).Assembly;

        foreach (var type in productsAssembly.GetExportedTypes())
        {
            if (type.IsClass && !type.IsAbstract)
            {
                // Contribution types (inherit from SwapContributionBase)
                if (IsContributionType(type))
                    AddClass(type);

                // Instrument types (implement ISwapInstrument)
                if (typeof(ISwapInstrument).IsAssignableFrom(type))
                    AddClass(type);
            }
        }

        // ── Tier enums — still explicit since they're in Model.Common ──
        AddEnum<SwapTiers>();
        AddEnum<EmeaSwapTiers>();

        // ── Common types ──
        AddClass<ContributionEvent>();
        AddClass<AutoQuoteState>();

        // ── Legacy explicit exports retained as placeholders for the full app ──
        // Uncomment and resolve these against the real solution so the generated client
        // preserves the legacy surface where required.
        // AddClass<SwapLatamConstants>();
        // AddClass<SwapInflationConstants>();
        // AddClass<SwapAsiaLmContribution>();
        // AddClass<SwapAsiaLmInstrument>();
        // AddClass<SwapAsiaLmConstants>();
        // AddClass<SwissScandiesContribution>();
        // AddClass<SwissScandiesInstrument>();
        // AddClass<SwissScandiesConstants>();
        // AddClass<SwapCeemeaContribution>();
        // AddClass<SwapCeemeaInstrument>();
        // AddClass<SwapCeemeaConstants>();
        // AddEnum<BondTiers>();
        // AddEnum<TraderIntention>();
        // AddClass<SierraAnalytics>();
        // AddClass<TradeWebComposite>();

        // ── Bond types will be auto-discovered from their own Products assembly later ──
        // var bondProductsAssembly = typeof(BondMonitorContribution).Assembly;
        // ... same pattern
    }

    private static bool IsContributionType(Type type)
    {
        // Walk the inheritance chain looking for SwapContributionBase<,>
        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition().Name.StartsWith("SwapContributionBase"))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
