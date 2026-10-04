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


    public async Task<IActionResult> Trace(string destination, CancellationToken cancellationToken)
    {
      return Json(await service.TraceAsync(destination, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> DestinationsAsync(CancellationToken cancellationToken)
    {
      return Json(await service.GetDestinationsAsync(cancellationToken));
    }

  }
}