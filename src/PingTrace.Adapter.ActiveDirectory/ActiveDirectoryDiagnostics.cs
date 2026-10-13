namespace PingTrace.Adapter.ActiveDirectory;

/// <summary>
/// Contains selected domain information returned by a directory reader.
/// </summary>
/// <remarks>
/// A small data contract keeps PingTrace mapping independent of LDAP
/// response objects and allows local simulation without a domain controller.
/// </remarks>
public sealed class ActiveDirectoryDiagnostics
{
    /// <summary>
    /// Gets the DNS hostname reported by the responding domain controller.
    /// </summary>
    public required string DomainController { get; init; }

    /// <summary>
    /// Gets the domain's distinguished name.
    /// </summary>
    public required string DomainDistinguishedName { get; init; }

    /// <summary>
    /// Gets the name attribute of the domain object.
    /// </summary>
    /// <remarks>
    /// This is not necessarily the domain's DNS name or NetBIOS name.
    /// </remarks>
    public required string DomainObjectName { get; init; }
}