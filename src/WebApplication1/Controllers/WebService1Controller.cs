#nullable enable

using Microsoft.AspNetCore.Mvc;
using Patware.PingTrace.Core;

namespace WebApplication1.Controllers
{
  [ApiController]
  [Route("api/WebService1")]
  public sealed class WebService1Controller(IPingTrace pingTrace, IDiscovery discovery) : ControllerBase
  {

    [HttpGet("Ping")]
    public IActionResult Ping()
    {
      return Content(pingTrace.Ping(), "text/plain");
    }

    [HttpGet("Destinations")]
    public async Task<IActionResult> DestinationsAsync(CancellationToken cancellationToken)
    {
      return Ok(await discovery.GetDestinationsAsync(cancellationToken));
    }


    [HttpGet("Trace")]
    public async Task<IActionResult> TraceAsync(CancellationToken cancellationToken, [FromQuery] string? destination = null)
    {
      if (!Request.Query.ContainsKey("destination"))
      {
        return Ok(await pingTrace.TraceAsync(cancellationToken));
      }

      if (string.IsNullOrWhiteSpace(destination))
      {
        return BadRequest("Destination must not be empty.");
      }

      try
      {
        return Ok(await pingTrace.TraceAsync(destination, cancellationToken));
      }
      catch (KeyNotFoundException)
      {
        return NotFound("The requested destination is not known.");
      }

    }
  }
}