using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Products.SwapLatam;
using Microsoft.AspNetCore.Mvc;

namespace Bnpp.eRates.Swap.Contribution.Host.SwapLatam.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SwapLatamContributionController : SwapContributionController<SwapLatamInstrument>
{
    public SwapLatamContributionController(
        ILogger<SwapContributionController<SwapLatamInstrument>> logger,
        IInstrumentProvider<SwapLatamInstrument> instrumentProvider)
        : base(logger, instrumentProvider)
    {
    }
}
