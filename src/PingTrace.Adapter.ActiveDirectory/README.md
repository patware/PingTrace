# Active Directory adapter

`PingTrace.Adapter.ActiveDirectory` reports selected domain diagnostics through
an application-registered `IActiveDirectoryDiagnosticsReader`. It targets
.NET 10 and includes an LDAP implementation using
`System.DirectoryServices.Protocols`.

## Register a destination

Register the reader and its configured connection separately, then register
the destination:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PingTrace.Adapter.ActiveDirectory;
using PingTrace.Runtime;

// Register the application's configured LdapConnection separately.
services.AddScoped<LdapActiveDirectoryDiagnosticsReader>();

services.AddPingTrace(pingTrace =>
{
    pingTrace.AddActiveDirectoryDestination<LdapActiveDirectoryDiagnosticsReader>(
        "ActiveDirectory",
        @"CONTOSO\application-account");
});
```

The host supplies `services` and the connection's server, authentication,
credentials, transport protection, and timeout settings. The identity label
must describe that configuration and must not contain passwords or tokens.
Applications can implement the public reader interface to reuse another
directory access layer. The generic registration resolves that concrete reader
type from dependency injection.

With the ASP.NET Core endpoints mapped, request:

```http
GET /pingtrace/Trace?destination=ActiveDirectory
```

The response contains the local application's result followed by the directory
result. `TraceAll` invokes the connector as a leaf. Ping and local Trace do not
resolve the reader or access the directory.

## LDAP operation

`LdapActiveDirectoryDiagnosticsReader` performs these operations on the supplied
connection:

1. Reject `AuthType.Anonymous` and bind with the configured credentials.
2. Perform a base-scope RootDSE search for `dnsHostName` and `defaultNamingContext`.
3. Perform a base-scope search at that naming context using
   `(objectClass=domainDNS)`, reading the domain object's `name` attribute.

Each search must return exactly one entry. Required attributes must have one
nonblank string value. Reading the domain object exercises domain data access
beyond RootDSE metadata. This is a small diagnostic read, not a full replacement
for every property returned by PowerShell's `Get-ADDomain`.

## Result fields

| Field | Meaning |
| --- | --- |
| `Name` | Local destination name |
| `MachineName` | Domain controller DNS hostname returned by the reader |
| `Identity` | Fixed configured identity label; not a server-returned identity assertion |
| `Payload` | JSON-encoded string with `DomainDistinguishedName`, `DomainObjectName`, `IdentitySource`, and `Probe` |
| Timing | Includes reader resolution and the diagnostic operation |

`IdentitySource` is `ConfiguredAuthentication`; `Probe` is `DomainRead`.
`DomainObjectName` is the domain object's `name` attribute, not necessarily its
DNS or NetBIOS name. The payload remains a string under the current Core contract.

The connector rejects null or incomplete diagnostics. Reader, LDAP, and
cancellation failures propagate. The ASP.NET Core endpoint does not map directory
failures to HTTP authentication statuses; the host's exception handler normally
returns HTTP 500.

## Connection ownership and cancellation

The connector is scoped and defers reader resolution until tracing begins.
Neither the connector nor the LDAP reader disposes application-owned resources.
The host must manage their lifetimes and give the reader exclusive use of its
connection while binding and reading. Do not run concurrent operations on that
connection.

The built-in LDAP reader uses synchronous `Bind` and `SendRequest` calls.
Returning a Task does not make them asynchronous. Cancellation is checked
between operations but cannot interrupt a native call already executing.
Configure a finite connection timeout; it is not a total probe deadline.
PingTrace's cooperative TraceAll deadline cannot forcibly stop these calls.

Use the host's intended authentication and transport configuration. For example,
LDAPS requires a controller configured for it and a trusted matching server
certificate. Native LDAP dependencies and authentication support vary by host
platform and require integration verification.

## Develop without a domain controller

Production and simulation use the same connector:

```text
Production: PingTrace -> connector -> LDAP reader -> domain controller
Sample:     PingTrace -> connector -> simulated reader
```

The tests substitute the reader and verify mapping, deferred resolution,
original failure propagation, incomplete-result rejection, token forwarding,
and cancellation with a cooperative reader. They do not execute or validate
the LDAP reader's bind, queries, native response parsing, authentication,
permissions, or native cancellation behavior.

Run from the repository root:

```powershell
dotnet test --project tests/PingTrace.Adapter.ActiveDirectory.Tests/PingTrace.Adapter.ActiveDirectory.Tests.csproj --configuration Release -- --minimum-expected-tests 1
```

The Aspire sample has a separate simulated reader with configurable scenarios.
See the [AD simulation walkthrough](https://github.com/patware/Patware.PingTrace/blob/main/docs/development/GETTING-STARTED.md#exercise-the-simulated-active-directory-destination).
Real-domain verification remains separate; successful simulated tests do not
establish that the production LDAP integration works in a deployment.
