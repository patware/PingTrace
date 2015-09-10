using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PingTraceWebApp.Controllers
{
    public class PingTraceController : Controller
    {
        private readonly Services.ITracePingService _tracePingService;

        public PingTraceController(Services.ITracePingService tracePingService)
        {
            _tracePingService = tracePingService;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Ping()
        {
            return Content(_tracePingService.Ping());
        }

        public IActionResult Trace(string destination)
        {
            
            return Json(_tracePingService.Trace(destination));
        }

        public IActionResult Traces()
        {
            return Json(_tracePingService.Traces());
        }
    }
}
