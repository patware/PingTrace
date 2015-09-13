using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.Rendering;

namespace PingTraceWebApp.Models
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

    public class TraceResult
    {
        public string Id { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }        
        public TimeSpan Elapsed { get { return TimeEnd - TimeStart; } }
        public string Destination { get; set; }
        public string Identity { get; set; }
        public string MachineName { get; set; }
        public string Payload { get; set; }
    }
}
