using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class PingTraceController : Controller
    {
        private Patware.PingTrace.Core.ITracePingService _tracePingService = new Services.PingTraceService();
        
        // GET: PingTrace
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ping()
        {
            return Content("Pong");
        }

        public ActionResult Trace(string destination)
        {
            return Json(_tracePingService.Trace(destination), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Traces()
        {
            return Json(_tracePingService.Traces(), JsonRequestBehavior.AllowGet);
        }
    }
}