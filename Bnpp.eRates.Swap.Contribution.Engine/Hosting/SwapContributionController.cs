using Bnpp.eRates.Contribution.Model.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bnpp.eRates.Swap.Contribution.Engine.Hosting;

/// <summary>
/// Generic contribution REST controller implementation.
/// ASP.NET does not discover open generic controllers, so each host still needs a
/// thin concrete subclass that closes TInstrument and inherits this action.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public class SwapContributionController<TInstrument> : ControllerBase
    where TInstrument : class, IInstrument
{
    private readonly ILogger _logger;
    private readonly IInstrumentProvider<TInstrument> _instrumentProvider;

    public SwapContributionController(
        ILogger<SwapContributionController<TInstrument>> logger,
        IInstrumentProvider<TInstrument> instrumentProvider)
    {
        _logger = logger;
        _instrumentProvider = instrumentProvider;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TInstrument>> GetInstruments()
    {
        _logger.LogInformation("Getting all instruments.");
        return Ok(_instrumentProvider.GetAllInstruments());
    }
}
