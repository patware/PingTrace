using Microsoft.Data.SqlClient;
using Patware.PingTrace.Core;
using System.Data;

namespace WebApplication1.Data
{
  public sealed class Repository(IConfiguration configuration) : IRepository
  {
    private readonly string _connectionString = configuration.GetConnectionString("Database1")
        ?? throw new InvalidOperationException(
          "ConnectionStrings:Database1 is not configured.");

    public async Task<string> RunPingAsync(CancellationToken cancellationToken)
    {
      using SqlConnection connection = new(_connectionString);
      using SqlCommand command =
        new("[PingTrace].[spPing]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      await connection.OpenAsync(cancellationToken);
      return await command.ExecuteScalarAsync(cancellationToken) as string ?? string.Empty;
    }

    public async Task<IReadOnlyList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken)
    {
      using SqlConnection connection = new(_connectionString);
      using SqlCommand command =
        new("[PingTrace].[spTraces]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      await connection.OpenAsync(cancellationToken);
      using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

      List<TraceDestination> destinations = [];

      while (await reader.ReadAsync(cancellationToken))
      {
        destinations.Add(new TraceDestination(reader.GetGuid(0), reader.GetString(1))
        {
          ExpectedIdentity = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
          ExpectedMachineNames = [reader.GetString(3)],
          ExpectedElapsedMilisecondsMax = reader.GetInt32(4),
          ExpectedElapsedMilisecondsAverage = reader.GetInt32(5),
          PayloadDescription = reader.GetString(6),
          PayloadRegex = reader.GetString(7)
        });
      }

      return destinations;
    }


    public async Task<TraceResult> RunTraceAsync(CancellationToken cancellationToken)
    {

      TraceResult result = new();

      using SqlConnection connection = new(_connectionString);
      using SqlCommand command =
        new("[PingTrace].[spTrace]", connection)
        {
          CommandType = CommandType.StoredProcedure
        };

      command.Parameters.Add("@destination", SqlDbType.NVarChar, 50).Value = "Database1";

      await connection.OpenAsync(cancellationToken);
      using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

      if (!await reader.ReadAsync(cancellationToken))
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

    public async Task<IReadOnlyList<TraceResult>> RunTraceAsync(string destination, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ArgumentException.ThrowIfNullOrWhiteSpace(destination);

      if (!string.Equals(destination, "Database1", StringComparison.OrdinalIgnoreCase))
      {
        throw new KeyNotFoundException(
          $"Unknown database trace destination '{destination}'.");
      }

      TraceResult result = await RunTraceAsync(cancellationToken);
      return [result];
    }

  }
}