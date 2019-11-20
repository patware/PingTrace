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
            Discover();
            TraceAndReportOnDatabase();
            Console.WriteLine("Finished... press enter");
            Console.ReadLine();
        }
        
        private static void Discover()
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

            foreach (var traceDestination in traces)
            {
                Console.WriteLine();
                Console.WriteLine(traceDestination.Name);

                var s = c.DownloadString(string.Format("http://localhost:4743/PingTrace/Trace?destination={0}", traceDestination.Name));

                var traceResults = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<Patware.PingTrace.Core.TraceResult>>(s);

                foreach (var traceResult in traceResults)
                {
                    Console.Write("\t");
                    Console.WriteLine(traceResult.Name);
                    Console.WriteLine("\tExpected Time: (max/avg) ({0}/{1}, actual: {2})", traceDestination.ExpectedElapsedMilisecondsMax, traceDestination.ExpectedElapsedMilisecondsAverage, traceResult.Elapsed.TotalMilliseconds);
                    Console.WriteLine("\tExpected Identity: {0} actual: {1}", traceDestination.ExpectedIdentity, traceResult.Identity);
                    Console.WriteLine("\tExpected MachineName: {0} actual: {1}", string.Join(",", traceDestination.ExpectedMachineNames), traceResult.MachineName);

                    Console.WriteLine("\tPayload Description: {0}", traceDestination.PayloadDescription);
                    Console.WriteLine("\tPayload: {0}", traceResult.Payload);
                    Console.WriteLine();

                }
            }

            c.Dispose();
        }


        private static void TraceAndReportOnDatabase()
        {
            var c = new System.Net.WebClient();

            var s = c.DownloadString("http://localhost:4743/PingTrace/Trace?destination=Database1");

            var traceResults = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<Patware.PingTrace.Core.TraceResult>>(s);

            // using a dictionary just it makes finding easier
            var dic = new Dictionary<string, Patware.PingTrace.Core.TraceResult>(traceResults.Count);
            
            foreach(var tr in traceResults)
            {
                dic.Add(tr.Name, tr);
            }

            var webTr = dic["WebApplication1"];
            var mvcTr = dic["WebService1"];
            var dbTr = dic["Database1"];

            Validate(webTr.Identity, Properties.Settings.Default.MvcExpectedIdentity, "Web Application's Identity not as expected.");
            Validate(webTr.MachineName, Properties.Settings.Default.MvcExpectedMachineName, "Web Application's MachineName not as expected.");
            Validate(webTr.Payload, "Pong [Database1]", "Web Application's Payload not as expected.");
            Validate(webTr.Elapsed.TotalMilliseconds, Properties.Settings.Default.MvcExpectedElapsedMsMax, "Web Application's Trace took more time than expected.");

            Validate(mvcTr.Identity, Properties.Settings.Default.WsExpectedIdentity, "Web Service's Identity not as expected.");
            Validate(mvcTr.MachineName, Properties.Settings.Default.WsExpectedMachineName, "Web Service's MachineName not as expected.");
            Validate(mvcTr.Payload, "Pong [Database1]", "Web Service's Payload not as expected.");
            Validate(mvcTr.Elapsed.TotalMilliseconds, Properties.Settings.Default.WsExpectedElapsedMsMax, "Web Service's Trace took more time than expected.");

            Validate(dbTr.Identity, Properties.Settings.Default.DbExpectedIdentity, "Db's Identity not as expected.");
            Validate(dbTr.MachineName, Properties.Settings.Default.DbExpectedMachineName, "Db's MachineName not as expected.");
            Validate(dbTr.Payload, "Pong [Database1]", "Db's Payload not as expected.");
            Validate(dbTr.Elapsed.TotalMilliseconds, Properties.Settings.Default.DbExpectedElapsedMsMax, "Db's Trace took more time than expected.");

            c.Dispose();

        }

        private static void Validate(string actual, string expected, string message)
        {
            if (expected != actual)
            {
                Console.WriteLine("{0}: Expecting {1}, Actual: {2}", message, expected, actual);
            }

        }
        private static void Validate(double actual, double expected, string message)
        {
            if (actual > expected)
            {
                Console.WriteLine("{0}: Expecting {1}, Actual: {2}", message, expected, actual);
            }

        }
    }
}
