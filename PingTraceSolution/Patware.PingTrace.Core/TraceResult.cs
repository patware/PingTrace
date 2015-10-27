using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
    
    public class TraceResult
    {
        public TraceResult()
        {
            this.Id = Guid.NewGuid();
            this.StartedAt = DateTime.Now;
        }
        public TraceResult(Guid id):this()
        {
            this.Id = id;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Identity { get; set; }
        public string MachineName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string Payload { get; set; }
          public void Finish(string payload)
        {
            this.Payload = payload;
            this.FinishedAt = DateTime.Now;            
        }      
        public TimeSpan Elapsed
        {
            get { return FinishedAt - StartedAt; }
        }

    }
}
