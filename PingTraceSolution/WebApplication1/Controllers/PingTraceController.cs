using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class PingTraceController : Controller
    {
        private Patware.PingTrace.Core.IPingTraceService _pingTraceService = new Services.PingTraceServiceForMvc();

        // GET: PingTrace
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ping()
        {
            return Content(_pingTraceService.Ping());
        }

        public ActionResult Traces()
        {
            return Json(_pingTraceService.Traces(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Trace(string destination)
        {
            return Json(_pingTraceService.Trace(destination), JsonRequestBehavior.AllowGet);
        }
    }
}