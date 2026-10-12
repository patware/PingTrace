using System.Data;
using System.Data.Common;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

using PingTrace.Core;
using PingTrace.Runtime;

namespace PingTrace.Adapter.EntityFramework.Tests;

/// <summary>
/// Verifies database destinations through the public registration API.
/// </summary>
/// <remarks>
/// SQLite provides relational query infrastructure without an external
/// server. A command interceptor suppresses execution of the SQL Server
/// query and supplies controlled results, failures, or cancellation.
///
/// These tests exercise result materialization and adapter behavior.
/// They do not validate SQL Server syntax, connectivity, or permissions.
/// </remarks>
[TestClass]
public sealed class DbContextPingTraceConnectorTests
{
    /// <summary>
    /// Gets or sets the context supplied by MSTest.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies database result mapping and cancellation-token forwarding.
    /// </summary>
    [TestMethod]
    public async Task SuccessfulQuery_ReturnsDatabaseResult()
    {
        // Arrange
        int calls = 0;
        CancellationToken observedToken = default;

        using ServiceProvider provider = CreateProvider(token =>
        {
            calls++;
            observedToken = token;

            return Task.FromResult(CreateReader());
        });

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut =
            scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        IReadOnlyList<TraceResult> results = await sut.TraceAsync(
            "DATABASE",
            TestContext.CancellationToken);

        // Assert
        calls.Should().Be(1);
        observedToken.Should().Be(TestContext.CancellationToken);

        results.Should().HaveCount(2);
        results[0].Name.Should().NotBeNullOrWhiteSpace();
        results[1].Name.Should().Be("Database");
        results[1].MachineName.Should().Be(@"TestServer\TestInstance");
        results[1].Identity.Should().Be("TestLogin");
        results[1].Payload.Should().Be("TestDatabase");
        results[1].StartedAt.Should().NotBe(default);
        results[1].FinishedAt.Should().NotBe(default);
    }

    /// <summary>
    /// Verifies that the original database exception reaches the caller.
    /// </summary>
    /// <remarks>
    /// Direct query execution no longer converts a false connectivity
    /// result into an adapter-generated exception. Preserving the original
    /// exception retains the provider's diagnostic information.
    /// </remarks>
    [TestMethod]
    public async Task FailedQuery_PropagatesOriginalException()
    {
        // Arrange
        StubDatabaseException failure = new("Database query failed.");

        using ServiceProvider provider = CreateProvider(_ =>
            Task.FromException<DbDataReader>(failure));

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut =
            scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Func<Task> act = () => sut.TraceAsync(
            "Database",
            TestContext.CancellationToken);

        // Assert
        var assertion =
            await act.Should().ThrowAsync<StubDatabaseException>();

        assertion.Which.Should().BeSameAs(failure);
    }

    /// <summary>
    /// Verifies that local diagnostics do not resolve a database context.
    /// </summary>
    [TestMethod]
    public async Task LocalOperations_DoNotResolveContext()
    {
        // Arrange
        ServiceCollection services = new();

        // Deliberately omit the context registration.
        services.AddPingTrace(builder =>
        {
            builder.AddDbContextDestination<StubContext>("Database");
        });

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut =
            scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        string ping = sut.Ping();
        TraceResult local =
            await sut.TraceAsync(TestContext.CancellationToken);

        // Assert
        ping.Should().Be("Pong");
        local.Name.Should().NotBeNullOrWhiteSpace();
        local.Payload.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies cancellation after query execution reaches the interceptor.
    /// </summary>
    /// <remarks>
    /// A completion signal ensures cancellation occurs during the query
    /// operation rather than before tracing begins. The intercepted
    /// operation waits on the token passed through EF Core.
    /// </remarks>
    [TestMethod]
    public async Task CancellationDuringQuery_Propagates()
    {
        // Arrange
        TaskCompletionSource<bool> entered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.CancellationToken);

        using ServiceProvider provider = CreateProvider(async token =>
        {
            entered.TrySetResult(true);

            await Task.Delay(Timeout.InfiniteTimeSpan, token);

            return CreateReader();
        });

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut =
            scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Task<IReadOnlyList<TraceResult>> pending =
            sut.TraceAsync("Database", cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
        }

        Func<Task> act = async () =>
        {
            await pending.WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.CancellationToken);
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ServiceProvider CreateProvider(
        Func<CancellationToken, Task<DbDataReader>> execute)
    {
        ServiceCollection services = new();

        StubCommandInterceptor interceptor = new(execute);

        services.AddDbContext<StubContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:");
            options.AddInterceptors(interceptor);
        });

        services.AddPingTrace(builder =>
        {
            builder.AddDbContextDestination<StubContext>("Database");
        });

        return BuildProvider(services);
    }

    private static ServiceProvider BuildProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    /// <summary>
    /// Creates a diagnostic row matching the current EF query projection.
    /// </summary>
    /// <remarks>
    /// The intercepted reader replaces the final command's result, so its
    /// column order must match EF's composed projection rather than the
    /// original SQL statement. For this query, that order is Identity,
    /// MachineName, and Payload.
    ///
    /// Assigning row values by column name keeps the test data readable
    /// while preserving the required reader ordinals.
    /// </remarks>
    private static DbDataReader CreateReader()
    {
        DataTable table = new();

        table.Columns.Add("Identity", typeof(string));
        table.Columns.Add("MachineName", typeof(string));
        table.Columns.Add("Payload", typeof(string));

        DataRow row = table.NewRow();
        row["Identity"] = "TestLogin";
        row["MachineName"] = @"TestServer\TestInstance";
        row["Payload"] = "TestDatabase";

        table.Rows.Add(row);

        return table.CreateDataReader();
    }

    private sealed class StubContext(
        DbContextOptions<StubContext> options) : DbContext(options)
    {
    }

    private sealed class StubCommandInterceptor(
        Func<CancellationToken, Task<DbDataReader>> execute)
        : DbCommandInterceptor
    {
        public override async ValueTask<InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            DbDataReader reader = await execute(cancellationToken);

            return InterceptionResult<DbDataReader>.SuppressWithResult(reader);
        }
    }

    private sealed class StubDatabaseException(string message)
        : DbException(message)
    {
    }
}