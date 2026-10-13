namespace PingTrace.Runtime;

/// <summary>
/// Defines configuration values that are not currently consumed by
/// the PingTrace runtime.
/// </summary>
/// <remarks>
/// The current runtime derives the application name from the managed
/// entry assembly. Configuring this type does not change runtime behavior.
/// </remarks>
public sealed class PingTraceOptions
{
    /// <summary>
    /// Gets or sets an application-name value.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty string. The current runtime does not read
    /// this property, so setting it does not override the application name.
    /// </remarks>
    public string Name { get; set; } = string.Empty;
}