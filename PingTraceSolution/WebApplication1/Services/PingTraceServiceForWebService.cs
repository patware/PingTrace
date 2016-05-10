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

        private readonly Data.IRepository _repo = new Data.Repository();

        private readonly Guid _thisId = new Guid("{32DE64E4-6FB4-4F8B-A2C5-C42BF1313141}");
        private readonly string _thisName = "WebService1";

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

            switch(destination.ToLowerInvariant())
            {
                case "database1":
                    l.Add(_repo.RunTrace(destination));
                    break;
            }

            tr.Finish(string.Format("Pong [{0}]", destination));

            return l;
        }

        public List<TraceDestination> Traces()
        {
            var l = new List<TraceDestination>();

            l.Add(new TraceDestination() {
                Id = _thisId
                , Name = _thisName
                ,ExpectedElapsedMilisecondsAverage = 1
                ,ExpectedElapsedMilisecondsMax = 1
                ,ExpectedIdentity = Properties.Settings.Default.ExpectedIdentityWebService
                ,ExpectedMachineNames = new[] { Properties.Settings.Default.ExpectedMachineNameWebService }
                ,PayloadDescription = "Pong and the destination in square brackets"
                ,PayloadRegex = "Pong \\[.*\\]"
        });

            l.AddRange(_repo.RunTraces());
            return l;
        }
    }
}
