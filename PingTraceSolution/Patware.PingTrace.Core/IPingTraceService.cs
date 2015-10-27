using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
    public interface IPingTraceService
    {
        string Ping();
        List<Patware.PingTrace.Core.TraceResult> Trace(string destination);
        List<Patware.PingTrace.Core.TraceDestination> Traces();
    }
}
