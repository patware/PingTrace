namespace PingTrace.Adapter.EntityFramework;

/// <summary>
/// Contains diagnostic values returned by SQL Server.
/// </summary>
/// <remarks>
/// A separate projection keeps database column mapping independent
/// of trace naming and timing properties.
/// </remarks>
internal sealed class DatabaseDiagnostics
{
    /// <summary>
    /// Gets or sets the SQL Server instance name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets the current SQL Server security context's login name.
    /// </summary>
    public string? Identity { get; set; }

    /// <summary>
    /// Gets or sets the current database name.
    /// </summary>
    public string? Payload { get; set; }
}