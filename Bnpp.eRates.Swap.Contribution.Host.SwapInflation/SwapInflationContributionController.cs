using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Products.SwapInflation;
using Microsoft.AspNetCore.Mvc;

namespace Bnpp.eRates.Swap.Contribution.Host.SwapInflation.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SwapInflationContributionController : SwapContributionController<SwapInflationInstrument>
{
    public SwapInflationContributionController(
        ILogger<SwapContributionController<SwapInflationInstrument>> logger,
        IInstrumentProvider<SwapInflationInstrument> instrumentProvider)
        : base(logger, instrumentProvider)
    {
    }
}
