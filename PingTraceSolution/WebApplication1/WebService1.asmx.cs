using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebApplication1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private Patware.PingTrace.Core.IPingTraceService _pingService = new Services.PingTraceServiceForWebService();

        [WebMethod]
        public string Ping()
        {
            return _pingService.Ping();
        }

        [WebMethod]
        public List<Patware.PingTrace.Core.TraceResult> Trace(string destination)
        {
            return _pingService.Trace(destination);
        }

        [WebMethod]
        public List<Patware.PingTrace.Core.TraceDestination> Traces()
        {
            return _pingService.Traces();
        }

    }
}
