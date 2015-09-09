﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PingTraceWebApp.Controllers
{
    public class PingTraceController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Ping()
        {
            return Content("Pong");
        }

        public IActionResult Trace(string destination)
        {
            return Content("Trace");
        }

        public IActionResult Traces()
        {
            return Content("Traces");
        }
    }
}
