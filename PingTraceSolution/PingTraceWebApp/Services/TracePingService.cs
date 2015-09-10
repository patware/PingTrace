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

        public IList<Models.TraceResult> Trace(string destination)
        {
            var l = new List<Models.TraceResult>();
            l.Add(
                new Models.TraceResult {
                    Id = 1
                    , Destination = "Foo"
                    , Identity = "Me"
                    , MachineName = "It"
                    , TimeStart = DateTime.Now.AddSeconds(-5)
                    , TimeEnd = DateTime.Now
                    , Payload = "Pong"
                });

            return l;
        }

        public IList<Models.TraceDestination> Traces()
        {
            var l = new List<Models.TraceDestination>();

            l.Add(
                new Models.TraceDestination{
                    Id = 1
                    , Name = "Foo"
                    , ElapsedAverage = new TimeSpan(0,0,3)
                    , ElapsedMax = new TimeSpan(0,0,5)
                    , ExpectedIdentity = "Me"
                    , ExpectedMachineName = "It"
                    , PayloadDescription = "Ping"
                    , PayloadRegex = "Pong"
            });

            return l;
        }
    }

    public interface ITracePingService
    {
        string Ping();
        IList<Models.TraceResult> Trace(string destination);
        IList<Models.TraceDestination> Traces();
    }

}
