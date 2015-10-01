using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
    public class TraceDestination
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ExpectedIdentity { get; set; }
        public string ExpectedMachineName { get; set; }
        public TimeSpan ElapsedMax { get; set; }
        public TimeSpan ElapsedAverage { get; set; }
        public string PayloadDescription { get; set; }
        public string PayloadRegex { get; set; }
    }
}
