using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patware.PingTrace.Core;

namespace WebApplication1.Services
{
    public class PingTraceService : Patware.PingTrace.Core.IPingTraceService
    {
        private Guid _thisId = new Guid("{A110F236-5A01-440D-B5F1-12D8901642D3}");

        public string Ping()
        {
            return "Pong";
        }

        public IList<TraceResult> Trace(string destination)
        {
            var l = new List<TraceResult>();

            var tr = new TraceResult(_thisId);
            tr.Finish("Pong");
            l.Add(tr);

            return l;
        }

        public IList<TraceDestination> Traces()
        {
            var l = new List<TraceDestination>();

            l.Add(new TraceDestination { Id = _thisId.ToString() });

            return l;
        }
    }
}
