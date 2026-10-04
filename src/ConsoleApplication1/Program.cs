using Newtonsoft.Json;
using Patware.PingTrace.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
  internal static class Program
  {
    private static async Task Main()
    {
      using HttpClient client = new()
      {
        BaseAddress = new Uri("http://localhost:4743/")
      };

      await DiscoverAsync(client);
      await TraceAndReportOnDatabaseAsync(client);

      Console.WriteLine("Finished... press enter");
      Console.ReadLine();
    }

    private static async Task DiscoverAsync(HttpClient client)
    {
      string ping = await client.GetStringAsync("PingTrace/Ping");
      string destinationsJson = await client.GetStringAsync("PingTrace/Destinations");

      Console.WriteLine("Ping:");
      Console.WriteLine(ping);
      Console.WriteLine();

      Console.WriteLine("Traces:");
      Console.WriteLine(destinationsJson);
      Console.WriteLine();

      IList<TraceDestination> destinations = JsonConvert.DeserializeObject<IList<TraceDestination>>(destinationsJson)
        ?? throw new InvalidOperationException(
          "The server returned a null destinations response.");

      foreach (TraceDestination destination in destinations)
      {
        Console.WriteLine();
        Console.WriteLine(destination.Name);

        IList<TraceResult> results = await GetTraceResultsAsync(client, destination.Name);

        foreach (TraceResult result in results)
        {
          Console.Write("\t");
          Console.WriteLine(result.Name);

          Console.WriteLine(
            "\tExpected Time: (max/avg) ({0}/{1}, actual: {2})",
            destination.ExpectedElapsedMilisecondsMax,
            destination.ExpectedElapsedMilisecondsAverage,
            result.Elapsed.TotalMilliseconds);

          Console.WriteLine(
            "\tExpected Identity: {0} actual: {1}",
            destination.ExpectedIdentity,
            result.Identity);

          Console.WriteLine(
            "\tExpected MachineName: {0} actual: {1}",
            string.Join(",", destination.ExpectedMachineNames),
            result.MachineName);

          Console.WriteLine(
            "\tPayload Description: {0}",
            destination.PayloadDescription);

          Console.WriteLine("\tPayload: {0}", result.Payload);
          Console.WriteLine();
        }
      }
    }

    private static async Task TraceAndReportOnDatabaseAsync(HttpClient client)
    {
      IList<TraceResult> results = await GetTraceResultsAsync(client, "Database1");

      Dictionary<string, TraceResult> resultsByName = new(results.Count);

      foreach (TraceResult result in results)
      {
        resultsByName.Add(result.Name, result);
      }

      TraceResult webTrace = resultsByName["WebApplication1"];
      TraceResult serviceTrace = resultsByName["WebService1"];
      TraceResult databaseTrace = resultsByName["Database1"];

      Validate(
        webTrace.Identity,
        Properties.Settings.Default.MvcExpectedIdentity,
        "Web Application's Identity not as expected.");

      Validate(
        webTrace.MachineName,
        Properties.Settings.Default.MvcExpectedMachineName,
        "Web Application's MachineName not as expected.");

      Validate(
        webTrace.Payload,
        "Pong [Database1]",
        "Web Application's Payload not as expected.");

      Validate(
        webTrace.Elapsed.TotalMilliseconds,
        Properties.Settings.Default.MvcExpectedElapsedMsMax,
        "Web Application's Trace took more time than expected.");

      Validate(
        serviceTrace.Identity,
        Properties.Settings.Default.WsExpectedIdentity,
        "Web Service's Identity not as expected.");

      Validate(
        serviceTrace.MachineName,
        Properties.Settings.Default.WsExpectedMachineName,
        "Web Service's MachineName not as expected.");

      Validate(
        serviceTrace.Payload,
        "Pong [Database1]",
        "Web Service's Payload not as expected.");

      Validate(
        serviceTrace.Elapsed.TotalMilliseconds,
        Properties.Settings.Default.WsExpectedElapsedMsMax,
        "Web Service's Trace took more time than expected.");

      Validate(
        databaseTrace.Identity,
        Properties.Settings.Default.DbExpectedIdentity,
        "Db's Identity not as expected.");

      Validate(
        databaseTrace.MachineName,
        Properties.Settings.Default.DbExpectedMachineName,
        "Db's MachineName not as expected.");

      Validate(
        databaseTrace.Payload,
        "Pong [Database1]",
        "Db's Payload not as expected.");

      Validate(
        databaseTrace.Elapsed.TotalMilliseconds,
        Properties.Settings.Default.DbExpectedElapsedMsMax,
        "Db's Trace took more time than expected.");
    }

    private static async Task<IList<TraceResult>> GetTraceResultsAsync(HttpClient client, string destination)
    {
      string path = "PingTrace/Trace?destination="
        + Uri.EscapeDataString(destination);

      string json = await client.GetStringAsync(path);

      return JsonConvert.DeserializeObject<IList<TraceResult>>(json)
        ?? throw new InvalidOperationException(
          $"The server returned a null trace response for '{destination}'.");
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