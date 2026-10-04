using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
  public class HomeController : Controller
  {
    // GET: Home
    public IActionResult Index()
    {
      return View();
    }
  }
}