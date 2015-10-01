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
        IList<Patware.PingTrace.Core.TraceResult> Trace(string destination);
        IList<Patware.PingTrace.Core.TraceDestination> Traces();
    }
}
