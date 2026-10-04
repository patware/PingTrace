#nullable enable

using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
  [ApiController]
  [Route("api/WebService1")]
  public sealed class WebService1Controller(PingTraceServiceForWebService service) : ControllerBase
  {

    [HttpGet("Ping")]
    public IActionResult Ping()
    {
      return Content(service.Ping(), "text/plain");
    }

    [HttpGet("Destinations")]
    public async Task<IActionResult> DestinationsAsync(CancellationToken cancellationToken)
    {
      return Ok(await service.GetDestinationsAsync(cancellationToken));
    }


    [HttpGet("Trace")]
    public async Task<IActionResult> TraceAsync(CancellationToken cancellationToken, [FromQuery] string? destination = null)
    {
      if (!Request.Query.ContainsKey("destination"))
      {
        return Ok(await service.TraceAsync(cancellationToken));
      }

      if (string.IsNullOrWhiteSpace(destination))
      {
        return BadRequest("Destination must not be empty.");
      }

      try
      {
        return Ok(await service.TraceAsync(destination, cancellationToken));
      }
      catch (KeyNotFoundException)
      {
        return NotFound("The requested destination is not known.");
      }

    }
  }
}