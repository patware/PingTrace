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

    [HttpGet("Traces")]
    public IActionResult Traces()
    {
      return Ok(service.Traces());
    }

    [HttpGet("Trace")]
    public IActionResult Trace([FromQuery] string destination)
    {
      return Ok(service.Trace(destination));
    }
  }
}