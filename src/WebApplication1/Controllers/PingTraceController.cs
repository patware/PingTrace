using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
  public sealed class PingTraceController(Services.PingTraceServiceForMvc service) : Controller
  {

    // GET: PingTrace
    [HttpGet]
    public IActionResult Index()
    {
      return View();
    }

    [HttpGet]
    public IActionResult Ping()
    {
      return Content(service.Ping(), "text/plain");
    }

    [HttpGet]
    public async Task<IActionResult> Traces(CancellationToken cancellationToken)
    {
      return Json(await service.TracesAsync(cancellationToken));
    }

    public async Task<IActionResult> Trace(string destination, CancellationToken cancellationToken)
    {
      return Json(await service.TraceAsync(destination, cancellationToken));
    }
  }
}