using Microsoft.Data.SqlClient;
using Patware.PingTrace.Core;
using System.Data;

namespace WebApplication1.Data
{
  public sealed class Repository : IRepository
  {
    private readonly string _connectionString;

    public Repository(IConfiguration configuration)
    {
      _connectionString = configuration.GetConnectionString("Database1")
        ?? throw new InvalidOperationException(
          "ConnectionStrings:Database1 is not configured.");
    }

    public string RunPing()
    {
      using SqlConnection connection = new SqlConnection(_connectionString);
      using SqlCommand command =
        new SqlCommand("[PingTrace].[spPing]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      connection.Open();
      return command.ExecuteScalar() as string ?? string.Empty;
    }

    public IList<TraceDestination> RunTraces()
    {
      using SqlConnection connection = new SqlConnection(_connectionString);
      using SqlCommand command =
        new SqlCommand("[PingTrace].[spTraces]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      connection.Open();
      using SqlDataReader reader = command.ExecuteReader();

      List<TraceDestination> destinations = new List<TraceDestination>();

      while (reader.Read())
      {
        destinations.Add(new TraceDestination(
          reader.GetGuid(0), reader.GetString(1))
        {
          ExpectedIdentity = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
          ExpectedMachineNames = new[] { reader.GetString(3) },
          ExpectedElapsedMilisecondsMax = reader.GetInt32(4),
          ExpectedElapsedMilisecondsAverage = reader.GetInt32(5),
          PayloadDescription = reader.GetString(6),
          PayloadRegex = reader.GetString(7)
        });
      }

      return destinations;
    }

    public TraceResult RunTrace(string destination)
    {
      if (string.IsNullOrWhiteSpace(destination) || destination.Length > 50)
      {
        throw new ArgumentException("Destination must contain between 1 and 50 characters.", nameof(destination));
      }

      TraceResult result = new TraceResult();

      using SqlConnection connection = new SqlConnection(_connectionString);
      using SqlCommand command =
        new SqlCommand("[PingTrace].[spTrace]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      command.Parameters.Add("@destination", SqlDbType.NVarChar, 50).Value = destination;

      connection.Open();
      using SqlDataReader reader = command.ExecuteReader();

      if (!reader.Read())
      {
        throw new InvalidOperationException("The database trace procedure returned no result.");
      }

      result.Id = reader.GetGuid(0);
      result.Name = reader.GetString(1);
      result.Identity = reader.GetString(2);
      result.MachineName = reader.GetString(3);
      result.Finish(reader.GetString(4));

      return result;
    }
  }
}