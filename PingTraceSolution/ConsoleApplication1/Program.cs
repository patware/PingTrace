using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {

            var c = new System.Net.WebClient();
            var p = c.DownloadString("http://localhost:4743/PingTrace/Ping");
            var ts = c.DownloadString("http://localhost:4743/PingTrace/Traces");
            
            Console.WriteLine("Ping:");
            Console.WriteLine(p);
            Console.WriteLine();

            Console.WriteLine("Traces:");
            Console.WriteLine(ts);
            Console.WriteLine();
                        
            
            var traces = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<Patware.PingTrace.Core.TraceDestination>>(ts);

            foreach(var traceDestination in traces)
            {
                Console.WriteLine();
                Console.WriteLine(traceDestination.Name);

                var s = c.DownloadString(string.Format("http://localhost:4743/PingTrace/Trace?destination={0}", traceDestination.Name));

                var traceResults = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<Patware.PingTrace.Core.TraceResult>>(s);
                
                foreach (var traceResult in traceResults)
                {
                    Console.Write("\t");
                    Console.WriteLine(traceResult.Name);
                    Console.WriteLine("\tExpected Time: (max/avg) ({0}/{1}, actual: {2})", traceDestination.ElapsedMaxSeconds, traceDestination.ElapsedAverageSeconds, traceResult.Elapsed.TotalSeconds);
                    Console.WriteLine("\tExpected Identity: {0} actual: {1}", traceDestination.ExpectedIdentity, traceResult.Identity);
                    Console.WriteLine("\tExpected MachineName: {0} actual: {1}", traceDestination.ExpectedMachineName, traceResult.MachineName);

                    Console.WriteLine("\tPayload Description: {0}", traceDestination.PayloadDescription);
                    Console.WriteLine("\tPayload: {0}", traceResult.Payload);
                    Console.WriteLine();
                }
            }

            c.Dispose();
        }
    }
}
