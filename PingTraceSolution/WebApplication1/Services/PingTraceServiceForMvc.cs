﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patware.PingTrace.Core;

namespace WebApplication1.Services
{
    public class PingTraceServiceForMvc : Patware.PingTrace.Core.IPingTraceService
    {
        private Guid _thisId = new Guid("{A110F236-5A01-440D-B5F1-12D8901642D3}");
        private string _thisName = "WebApplication1";

        public string Ping()
        {
            return "Pong";
        }

        public List<TraceResult> Trace(string destination)
        {
            var l = new List<TraceResult>();

            var tr = new TraceResult(id:_thisId);
            tr.Name = _thisName;
            tr.Finish("Pong");
            l.Add(tr);

            return l;
        }

        public List<TraceDestination> Traces()
        {
            var l = new List<TraceDestination>();
                        
            l.Add(new TraceDestination(id:_thisId, name:_thisName));

            return l;
        }
    }
}