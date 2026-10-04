using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patware.PingTrace.Core;

namespace WebApplication1.Services
{
    public class PingTraceServiceForMvc : Patware.PingTrace.Core.IPingTraceService
    {
        private readonly Guid _thisId = new Guid("{A110F236-5A01-440D-B5F1-12D8901642D3}");
        private readonly string _thisName = "WebApplication1";
        
        public string Ping()
        {
            return "Pong";
        }

        public List<TraceResult> Trace(string destination)
        {
            var l = new List<TraceResult>();

            var tr = new TraceResult(id: _thisId);
            tr.Name = _thisName;
            tr.MachineName = System.Environment.MachineName;
            tr.Identity = System.Environment.UserDomainName;
            
            l.Add(tr);

            if (!string.IsNullOrEmpty(destination) && destination.ToLowerInvariant() !=  _thisName.ToLowerInvariant())
            {
                var mws = new MyWebService1.WebService1SoapClient();
                var res = mws.Trace(destination);
                foreach(var re in res)
                {
                    l.Add(new TraceResult() {
                        Id = re.Id
                        , FinishedAt = re.FinishedAt
                        , Identity = re.Identity
                        , MachineName = re.MachineName
                        , Name = re.Name
                        , Payload = re.Payload
                        , StartedAt = re.StartedAt });
                }
            }

            tr.Finish(string.Format("Pong [{0}]", destination));

            return l;
        }

        public List<TraceDestination> Traces()
        {
            var l = new List<TraceDestination>();
            var td = new TraceDestination(id: _thisId, name: _thisName);
            td.ExpectedElapsedMilisecondsAverage = 1;
            td.ExpectedElapsedMilisecondsMax = 1;
            td.ExpectedIdentity = Properties.Settings.Default.ExpectedIdentityMvc;
            td.ExpectedMachineNames = new[] { Properties.Settings.Default.ExpectedMachineNameMvc };
            td.PayloadDescription = "Pong and the destination in square brackets";
            td.PayloadRegex = "Pong \\[.*\\]";
            l.Add(td);
            
            var mws = new MyWebService1.WebService1SoapClient();
            var traces = mws.Traces();

            foreach(var traceDestination in traces)
            {
                l.Add(new TraceDestination() {
                    Id = traceDestination.Id
                    , Name = traceDestination.Name
                    , ExpectedElapsedMilisecondsAverage = traceDestination.ExpectedElapsedMilisecondsAverage
                    , ExpectedElapsedMilisecondsMax = traceDestination.ExpectedElapsedMilisecondsMax
                    , ExpectedIdentity = traceDestination.ExpectedIdentity
                    , ExpectedMachineNames = traceDestination.ExpectedMachineNames.ToArray()
                    , PayloadDescription = traceDestination.PayloadDescription
                    , PayloadRegex = traceDestination.PayloadRegex
                });
            }

            return l;
        }
    }
}
