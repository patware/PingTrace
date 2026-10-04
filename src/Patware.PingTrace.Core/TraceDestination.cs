using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
    public class TraceDestination
    {
        public TraceDestination()
        {
            this.Id = Guid.NewGuid();
        }
        public TraceDestination(Guid id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        /// <summary>
        /// Unique id, whatever the "project"
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Friendly name of the target for this "project"
        /// </summary>
        public string Name { get; set; }
        public string ExpectedIdentity { get; set; }
        /// <summary>
        /// List of Machines names because of load balancing scenarios...
        /// </summary>
        /// <remarks>Try semi-hard-coding this, like with a config file so that when the 
        /// actual trace is called, the current running values are returned</remarks>
        public string[] ExpectedMachineNames { get; set; }
        public int ExpectedElapsedMilisecondsMax { get; set; }
        public int ExpectedElapsedMilisecondsAverage { get; set; }
        /// <summary>
        /// Used to explain in plain words what to expect in the payload
        /// </summary>
        public string PayloadDescription { get; set; }
        /// <summary>
        /// A regex string that will be used to validate the payload.
        /// </summary>
        public string PayloadRegex { get; set; }
    }
}
