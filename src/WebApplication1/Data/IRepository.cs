using System.Collections.Generic;
using Patware.PingTrace.Core;

namespace WebApplication1.Data
{
    public interface IRepository
    {
        string RunPing();
        TraceResult RunTrace(string destination);
        IList<TraceDestination> RunTraces();
    }
}