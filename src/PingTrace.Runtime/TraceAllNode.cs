namespace PingTrace.Runtime;

/// <summary>
/// Identifies one running PingTrace application for cycle detection.
/// </summary>
/// <remarks>
/// Register this type as a singleton so separate request scopes share
/// the same identity. The token is not a permanent application identifier
/// and changes when the service provider is recreated.
///
/// Separate application instances have separate tokens. The depth limit
/// therefore remains necessary when calls pass through load balancers.
/// </remarks>
public sealed class TraceAllNode
{
    /// <summary>
    /// Gets the identifier of this running application.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}