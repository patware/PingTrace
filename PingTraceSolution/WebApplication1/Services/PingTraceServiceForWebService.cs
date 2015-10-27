using Patware.PingTrace.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class PingTraceServiceForWebService : Patware.PingTrace.Core.IPingTraceService
    {
        private Guid _thisId = new Guid("{32DE64E4-6FB4-4F8B-A2C5-C42BF1313141}");
        private string _thisName = "WebService1";

        public string Ping()
        {
            return "Pong";
        }

        public List<TraceResult> Trace(string destination)
        {
            var l = new List<TraceResult>();

            var tr = new TraceResult(id: _thisId);
            tr.Name = _thisName;
            tr.Finish("Pong");
            l.Add(tr);

            return l;
        }

        public List<TraceDestination> Traces()
        {
            var l = new List<TraceDestination>();

            l.Add(new TraceDestination(id: _thisId, name: _thisName));

            return l;
        }
    }
}
