using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PingTraceWebApp.Services
{
    public class TracePingService : ITracePingService
    {
        public string Ping()
        {
            
            return "Pong";
        }

        public IList<Patware.PingTrace.Core.TraceResult> Trace(string destination)
        {
            var l = new List<Patware.PingTrace.Core.TraceResult>();
            l.Add(
                new Patware.PingTrace.Core.TraceResult
                {
                    Id = "Foo.Web" ,
                    Destination = "Foo",
                    Identity = "Me",
                    MachineName = "It",
                    TimeStart = DateTime.Now.AddSeconds(-5),
                    TimeEnd = DateTime.Now,
                    Payload = "Pong"
                });

            return l;
        }

        public IList<Patware.PingTrace.Core.TraceDestination> Traces()
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

    public interface ITracePingService
    {
        string Ping();
        IList<Patware.PingTrace.Core.TraceResult> Trace(string destination);
        IList<Patware.PingTrace.Core.TraceDestination> Traces();
    }

}
