using FluentAssertions;

using NSubstitute;

using PingTrace.Core;

namespace PingTrace.Runtime.Tests;

/// <summary>
/// Verifies recursive traversal and cycle detection.
/// </summary>
[TestClass]
public sealed class TraceAllTests
{
    /// <summary>
    /// Gets or sets the context supplied by MSTest.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies recursive connectors and leaf connectors execute in order.
    /// </summary>
    [TestMethod]
    public async Task TraceAll_VisitsRecursiveAndLeafDestinations()
    {
        // Arrange
        List<string> calls = new();
        TraceResult remote = new() { Name = "RemoteApi" };
        TraceResult database = new() { Name = "Database" };
        TraceResult leafResult = new() { Name = "LocalDatabase" };

        remote.Finish("Remote");
        database.Finish("Database");
        leafResult.Finish("Local database");

        IRecursivePingTraceConnector recursive = Substitute.For<IRecursivePingTraceConnector>();
        recursive.Name.Returns("A-Api");

        TraceAllRequest? observedRequest = null;

        recursive.TraceAllAsync(
                Arg.Any<TraceAllRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add("recursive");
                observedRequest = call.Arg<TraceAllRequest>();

                return Task.FromResult<IReadOnlyList<TraceResult>>(
                    new[] { remote, database });
            });

        IPingTraceConnector leaf =
            Substitute.For<IPingTraceConnector>();
        leaf.Name.Returns("B-Database");

        leaf.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("leaf");

                return Task.FromResult<IReadOnlyList<TraceResult>>(
                    new[] { leafResult });
            });

        TraceAllNode node = new();
        DefaultPingTrace sut = new(
            new IPingTraceConnector[] { leaf, recursive },
            node);

        TraceAllRequest request = new();

        // Act
        IReadOnlyList<TraceResult> results = await sut.TraceAllAsync(request, TestContext.CancellationToken);

        // Assert
        calls.Should().Equal("recursive", "leaf");
        results.Should().HaveCount(4);
        results[0].FinishedAt.Should().NotBe(default);
        results.Skip(1).Should().Equal(remote, database, leafResult);

        observedRequest.Should().NotBeNull();
        observedRequest!.RemainingDepth.Should()
            .Be(request.RemainingDepth - 1);
        observedRequest.Ancestors.Should().Equal(node.Id);
        observedRequest.DeadlineUtc.Should().Be(request.DeadlineUtc);
        request.Ancestors.Should().BeEmpty();

        await recursive.DidNotReceive()
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that revisiting a node fails before invoking dependencies.
    /// </summary>
    [TestMethod]
    public async Task TraceAll_RevisitedNode_DoesNotInvokeConnectors()
    {
        // Arrange
        TraceAllNode node = new();

        IPingTraceConnector connector =
            Substitute.For<IPingTraceConnector>();
        connector.Name.Returns("Database");

        DefaultPingTrace sut = new(new[] { connector }, node);

        TraceAllRequest request = new()
        {
            Ancestors = new[] { node.Id }
        };

        // Act
        Func<Task> act = () => sut.TraceAllAsync(request, TestContext.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*cycle*");

        await connector.DidNotReceive()
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an application without destinations succeeds at zero depth.
    /// </summary>
    [TestMethod]
    public async Task TraceAll_LeafAtZeroDepth_ReturnsCompletedLocalResult()
    {
        // Arrange
        DefaultPingTrace sut = new();
        TraceAllRequest request = new() { RemainingDepth = 0 };

        // Act
        IReadOnlyList<TraceResult> results = await sut.TraceAllAsync(request, TestContext.CancellationToken);

        // Assert
        TraceResult result = results.Should().ContainSingle().Subject;
        result.Name.Should().NotBeNullOrWhiteSpace();
        result.StartedAt.Should().NotBe(default);
        result.FinishedAt.Should().NotBe(default);
        result.Payload.Should().Be(
            System.Reflection.Assembly.GetEntryAssembly()!
                .GetName().Version?.ToString() ?? "Pong");
    }

    /// <summary>
    /// Verifies that exhausted depth fails before invoking a destination.
    /// </summary>
    /// <remarks>
    /// A successful local-only response would incorrectly imply that the
    /// recursive traversal completed.
    /// </remarks>
    [TestMethod]
    public async Task TraceAll_ZeroDepthWithDestination_DoesNotInvokeConnector()
    {
        // Arrange
        IPingTraceConnector connector = Substitute.For<IPingTraceConnector>();
        connector.Name.Returns("Database");

        DefaultPingTrace sut = new(new[] { connector });
        TraceAllRequest request = new() { RemainingDepth = 0 };

        // Act
        Func<Task> act = () => sut.TraceAllAsync(request, TestContext.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*depth*");

        await connector.DidNotReceive()
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an expired deadline prevents dependency execution.
    /// </summary>
    /// <remarks>
    /// A deadline already in the past avoids timing-sensitive sleeps.
    /// </remarks>
    [TestMethod]
    public async Task TraceAll_ExpiredDeadline_DoesNotInvokeConnector()
    {
        // Arrange
        IPingTraceConnector connector = Substitute.For<IPingTraceConnector>();
        connector.Name.Returns("Database");

        DefaultPingTrace sut = new(new[] { connector });
        TraceAllRequest request = new()
        {
            DeadlineUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        // Act
        Func<Task> act = () => sut.TraceAllAsync(request, TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await connector.DidNotReceive().TraceAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that destinations execute sequentially and cancellation stops
    /// traversal while the first destination is pending.
    /// </summary>
    /// <param name="cancel">
    /// Whether to cancel instead of completing the first destination.
    /// </param>
    /// <remarks>
    /// An explicit completion source controls the dependency. The test does
    /// not depend on delays or elapsed-time thresholds.
    /// </remarks>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task TraceAll_PendingDestination_BlocksNextDestination(bool cancel)
    {
        // Arrange
        TaskCompletionSource<IReadOnlyList<TraceResult>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.CancellationToken);

        CancellationToken observedToken = default;

        IRecursivePingTraceConnector first =
            Substitute.For<IRecursivePingTraceConnector>();
        first.Name.Returns("A-Api");

        first.TraceAllAsync(
                Arg.Any<TraceAllRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedToken = call.Arg<CancellationToken>();
                return completion.Task.WaitAsync(observedToken);
            });

        IPingTraceConnector second = Substitute.For<IPingTraceConnector>();
        second.Name.Returns("B-Database");
        second.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TraceResult>>(
                new[] { new TraceResult { Name = "Database" } }));

        DefaultPingTrace sut = new(
            new IPingTraceConnector[] { second, first });

        // Act
        Task<IReadOnlyList<TraceResult>> pending = sut.TraceAllAsync(cancellationToken: cancellation.Token);

        bool completedBeforeDependency = pending.IsCompleted;
        int secondCallsBeforeCompletion =
            second.ReceivedCalls().Count(call =>
                call.GetMethodInfo().Name == nameof(IPingTraceConnector.TraceAsync));

        if (cancel)
        {
            cancellation.Cancel();
        }
        else
        {
            completion.SetResult(
                new[] { new TraceResult { Name = "RemoteApi" } });
        }

        // Assert
        completedBeforeDependency.Should().BeFalse();
        secondCallsBeforeCompletion.Should().Be(0);
        observedToken.CanBeCanceled.Should().BeTrue();

        if (cancel)
        {
            Func<Task> act = async () =>
            {
                await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
            observedToken.IsCancellationRequested.Should().BeTrue();

            await second.DidNotReceive()
                .TraceAsync(Arg.Any<CancellationToken>());
        }
        else
        {
            IReadOnlyList<TraceResult> results = await pending.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.CancellationToken);

            results.Skip(1).Select(result => result.Name)
                .Should().Equal("RemoteApi", "Database");

            results[0].FinishedAt.Should().NotBe(default);

            await second.Received(1)
                .TraceAsync(Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Verifies that a recursive failure propagates and prevents later calls.
    /// </summary>
    [TestMethod]
    public async Task TraceAll_ConnectorFailure_StopsTraversal()
    {
        // Arrange
        InvalidOperationException failure = new("Remote traversal failed.");

        IRecursivePingTraceConnector first = Substitute.For<IRecursivePingTraceConnector>();
        first.Name.Returns("A-Api");

        first.TraceAllAsync(
                Arg.Any<TraceAllRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<TraceResult>>(failure));

        IPingTraceConnector second = Substitute.For<IPingTraceConnector>();
        second.Name.Returns("B-Database");

        DefaultPingTrace sut = new(new IPingTraceConnector[] { first, second });

        // Act
        Func<Task> act = () => sut.TraceAllAsync(
            cancellationToken: TestContext.CancellationToken);

        // Assert
        var assertion = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        assertion.Which.Should().BeSameAs(failure);

        await second.DidNotReceive()
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that two branches can visit the same dependency.
    /// </summary>
    /// <remarks>
    /// Ancestor tracking belongs to each traversal path. A shared dependency
    /// must execute once per branch rather than being globally suppressed.
    /// </remarks>
    [TestMethod]
    public async Task TraceAll_SharedDependency_IsVisitedThroughEachBranch()
    {
        // Arrange
        IPingTraceConnector database = Substitute.For<IPingTraceConnector>();
        database.Name.Returns("Database");

        database.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                TraceResult result = new() { Name = "Database" };
                result.Finish("Database payload");

                return Task.FromResult<IReadOnlyList<TraceResult>>(
                    new[] { result });
            });

        DefaultPingTrace shared = new(new[] { database });

        DefaultPingTrace left = new(new[] { CreateRecursiveConnector("Shared", shared) });

        DefaultPingTrace right = new(new[] { CreateRecursiveConnector("Shared", shared) });

        DefaultPingTrace sut = new(new[]
        {
            CreateRecursiveConnector("Left", left),
            CreateRecursiveConnector("Right", right)
        });

        // Act
        IReadOnlyList<TraceResult> results = await sut.TraceAllAsync(
            cancellationToken: TestContext.CancellationToken);

        // Assert
        results.Should().HaveCount(7);
        results.Count(result => result.Name == "Database").Should().Be(2);
        results.Should().OnlyContain(result => result.FinishedAt != default);

        await database.Received(2)
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    private static IRecursivePingTraceConnector CreateRecursiveConnector(string name, IPingTrace destination)
    {
        IRecursivePingTraceConnector connector =
            Substitute.For<IRecursivePingTraceConnector>();

        connector.Name.Returns(name);
        connector.TraceAllAsync(
                Arg.Any<TraceAllRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call => destination.TraceAllAsync(
                call.Arg<TraceAllRequest>(),
                call.Arg<CancellationToken>()));

        return connector;
    }
}