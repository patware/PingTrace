using FluentAssertions;
using NSubstitute;
using Patware.PingTrace.Core;

namespace Patware.PingTrace.Tests;

[TestClass]
public sealed class DefaultPingTraceTests
{
  private static readonly Guid ApplicationId = Guid.Parse("A110F236-5A01-440D-B5F1-12D8901642D3");
  private static readonly string ApplicationName = "TestApp";

  [TestMethod]
  public void Ping_ReturnsPong()
  {
    DefaultPingTrace sut = Create();

    var result = sut.Ping();

    result.Should().Be("Pong");

  }

  [TestMethod]
  public async Task LocalTrace_ReturnsApplicationDetails()
  {
    DefaultPingTrace sut = Create();

    TraceResult result = await sut.TraceAsync(TestContext.CancellationToken);

    result.Id.Should().Be(ApplicationId);
    result.Name.Should().Be(ApplicationName);

    result.MachineName.Should().Be(Environment.MachineName);
    result.Identity.Should().Be($"{Environment.UserDomainName}\\{Environment.UserName}");
    result.Payload.Should().Be("Pong");
    result.FinishedAt.Should().NotBe(default);

  }

  [TestMethod]
  public async Task NamedLocalTrace_ReturnsOnlyApplication()
  {
    DefaultPingTrace sut = Create();
    string destination = ApplicationName.ToUpperInvariant();

    IReadOnlyList<TraceResult> results = await sut.TraceAsync(destination, TestContext.CancellationToken);

    TraceResult result = results.Should().ContainSingle().Subject;

    result.Id.Should().Be(ApplicationId);
    result.Name.Should().Be(ApplicationName);
    result.Payload.Should().Be($"Pong [{destination}]");
    result.FinishedAt.Should().NotBe(default);

  }

  [TestMethod]
  public async Task DestinationTrace_PreservesOrderAndPassesCancellation()
  {
    CancellationToken cancellationToken = TestContext.CancellationToken;

    TraceResult serviceResult = new()
    {
      Name = "RemoteService"
    };

    serviceResult.Finish("Pong");

    TraceResult databaseResult = new()
    {
      Name = "Database1"
    };

    databaseResult.Finish("Pong");

    IPingTraceDestination destination = Substitute.For<IPingTraceDestination>();

    destination.Name.Returns("Database1");

    destination
      .TraceAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<TraceResult>>([serviceResult, databaseResult]));

    DefaultPingTrace sut = Create(destination);

    IReadOnlyList<TraceResult> results = await sut.TraceAsync("database1", cancellationToken);

    results.Should().HaveCount(3);
    results.Select(result => result.Name).Should().Equal(ApplicationName, "RemoteService", "Database1");

    results[0].Id.Should().Be(ApplicationId);
    results[0].Payload.Should().Be("Pong [database1]");
    results[0].FinishedAt.Should().NotBe(default);

    results[1].Should().BeSameAs(serviceResult);
    results[2].Should().BeSameAs(databaseResult);

    await destination.Received(1).TraceAsync(cancellationToken);

  }

  [TestMethod]
  public async Task UnknownDestination_ThrowsWithoutCallingHandler()
  {
    IPingTraceDestination destination = Substitute.For<IPingTraceDestination>();

    destination.Name.Returns("Database1");

    DefaultPingTrace sut = Create(destination);

    Func<Task> act = () => sut.TraceAsync("Missing", TestContext.CancellationToken);

    await act.Should().ThrowAsync<KeyNotFoundException>();

    await destination.DidNotReceive().TraceAsync(Arg.Any<CancellationToken>());
  }

  [TestMethod]
  [DataRow(false)]
  [DataRow(true)]
  public async Task CancelledTrace_ThrowsWithoutCallingHandler(bool useDestination)
  {
    using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);

    cancellation.Cancel();

    IPingTraceDestination destination = Substitute.For<IPingTraceDestination>();

    destination.Name.Returns("Database1");

    DefaultPingTrace sut = Create(destination);

    Func<Task> act = async () =>
    {
      if (useDestination)
      {
        await sut.TraceAsync("Database1", cancellation.Token);
      }
      else
      {
        await sut.TraceAsync(cancellation.Token);
      }
    };

    await act.Should().ThrowAsync<OperationCanceledException>();

    await destination.DidNotReceive().TraceAsync(Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task DestinationFailure_PropagatesToCaller()
  {
    InvalidOperationException failure = new("Simulated downstream failure.");

    IPingTraceDestination destination = Substitute.For<IPingTraceDestination>();

    destination.Name.Returns("Database1");

    destination
      .TraceAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromException<IReadOnlyList<TraceResult>>(failure));

    DefaultPingTrace sut = Create(destination);

    Func<Task> act = () => sut.TraceAsync("Database1", TestContext.CancellationToken);

    var assertion = await act.Should().ThrowAsync<InvalidOperationException>();

    assertion.Which.Should().BeSameAs(failure);
  }

  [TestMethod]
  public async Task DestinationTrace_WaitsForDownstreamCompletion()
  {
    TaskCompletionSource<IReadOnlyList<TraceResult>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    IPingTraceDestination destination = Substitute.For<IPingTraceDestination>();

    destination.Name.Returns("Database1");

    destination
      .TraceAsync(Arg.Any<CancellationToken>())
      .Returns(completion.Task);

    DefaultPingTrace sut = Create(destination);

    Task<IReadOnlyList<TraceResult>> pending = sut.TraceAsync("Database1", TestContext.CancellationToken);

    pending.IsCompleted.Should().BeFalse();

    TraceResult databaseResult = new()
    {
      Name = "Database1"
    };

    databaseResult.Finish("Pong");

    completion.SetResult([databaseResult]);

    IReadOnlyList<TraceResult> results = await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

    results.Should().HaveCount(2);
    results[1].Should().BeSameAs(databaseResult);
    results[0].FinishedAt.Should().NotBe(default);
  }

  private static DefaultPingTrace Create(params IPingTraceDestination[] destinations)
  {
    return new DefaultPingTrace(new PingTraceOptions
    {
      Id = ApplicationId,
      Name = ApplicationName
    },
    destinations: destinations);
  }

  public TestContext TestContext { get; set; }
}
