using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patware.PingTrace.Core;

namespace WebApplication1.Services
{
    public class PingTraceService : Patware.PingTrace.Core.ITracePingService
    {
        public string Ping()
        {
            return "Pong";
        }

        public IList<TraceResult> Trace(string destination)
        {
            var l = new List<Patware.PingTrace.Core.TraceResult>();
            l.Add(
                new Patware.PingTrace.Core.TraceResult(id:new Guid("EDAF693F-643A-46D4-901E-3B991F78F07B"))
                {
                    Name = "Foo",
                    Identity = "Me",
                    MachineName = "It",
                    StartedAt = DateTime.Now.AddSeconds(-5),
                    FinishedAt = DateTime.Now,
                    Payload = "Pong"
                });

            return l;

        }

        public IList<TraceDestination> Traces()
        {
            var l = new List<Patware.PingTrace.Core.TraceDestination>();

            l.Add(
                new Patware.PingTrace.Core.TraceDestination
                {
                    Id = "Foo.Web",
                    Name = "Foo",
                    ElapsedAverage = new TimeSpan(0, 0, 3),
                    ElapsedMax = new TimeSpan(0, 0, 5),
                    ExpectedIdentity = "Me",
                    ExpectedMachineName = "It",
                    PayloadDescription = "Ping",
                    PayloadRegex = "Pong"
                });
            
            return l;

        }
    }
}
