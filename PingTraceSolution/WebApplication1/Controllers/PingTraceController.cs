using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class PingTraceController : Controller
    {
        // GET: PingTrace
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ping()
        {
            return Content("Pong");
        }

    }
}