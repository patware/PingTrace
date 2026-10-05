using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Patware.PingTrace.AspNetCore;
using Patware.PingTrace.Core;
using System.Net;
using System.Text.Json;

namespace Patware.PingTrace.Tests
{
  [TestClass]
  public sealed class PingTraceEndpointTests
  {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Ping_WorksWithoutDiscovery()
    {
      await using WebApplication app = await CreateApplicationAsync();

      using HttpClient client = app.GetTestClient();

      using HttpResponseMessage response = await client.GetAsync("/diagnostics/Ping", TestContext.CancellationToken);

      response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
      response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

      string body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

      body.Should().Be("Pong");

      using HttpResponseMessage discoveryResponse = await client.GetAsync(
        "/diagnostics/Destinations", TestContext.CancellationToken);

      discoveryResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DataRow("/diagnostics/Trace", false)]
    [DataRow("/diagnostics/Trace?destination=TestApp", true)]
    public async Task Trace_ReturnsExpectedJsonShape(string path, bool expectArray)
    {
      await using WebApplication app = await CreateApplicationAsync();
      using HttpClient client = app.GetTestClient();

      using HttpResponseMessage response = await client.GetAsync(path, TestContext.CancellationToken);

      response.StatusCode.Should().Be(HttpStatusCode.OK);

      string body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

      using JsonDocument json = JsonDocument.Parse(body);

      JsonElement root = json.RootElement;

      root.ValueKind.Should().Be(expectArray ? JsonValueKind.Array : JsonValueKind.Object);

      if (expectArray)
      {
        root.GetArrayLength().Should().Be(1);
      }

      JsonElement result = expectArray ? root[0] : root;

      result.GetProperty("Name").GetString().Should().Be("TestApp");
      result.TryGetProperty("name", out _).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("/diagnostics/Trace?destination=", 400)]
    [DataRow("/diagnostics/Trace?destination=%20", 400)]
    [DataRow("/diagnostics/Trace?destination=Missing", 404)]
    public async Task InvalidDestination_ReturnsExpectedStatus(string path, int expectedStatus)
    {
      await using WebApplication app = await CreateApplicationAsync();
      using HttpClient client = app.GetTestClient();

      using HttpResponseMessage response = await client.GetAsync(path, TestContext.CancellationToken);

      ((int)response.StatusCode).Should().Be(expectedStatus);
    }

    [TestMethod]
    public async Task Discovery_ReturnsRegisteredDestinations()
    {
      IDiscovery discovery = Substitute.For<IDiscovery>();

      TraceDestination destination = new(Guid.Parse("188712E4-4E7E-417E-8C91-D9115BC0BE22"), "Database1");

      discovery
        .GetDestinationsAsync(Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<IReadOnlyList<TraceDestination>>([destination]));

      await using WebApplication app = await CreateApplicationAsync(discovery);

      using HttpClient client = app.GetTestClient();

      using HttpResponseMessage response = await client.GetAsync("/diagnostics/Destinations", TestContext.CancellationToken);

      response.StatusCode.Should().Be(HttpStatusCode.OK);

      string body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

      using JsonDocument json = JsonDocument.Parse(body);

      json.RootElement.GetArrayLength().Should().Be(1);

      json.RootElement[0].GetProperty("Name").GetString()
        .Should().Be("Database1");

      await discovery.Received(1).GetDestinationsAsync(Arg.Any<CancellationToken>());
    }



    private async Task<WebApplication> CreateApplicationAsync(IDiscovery? discovery = null)
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder();

      builder.WebHost.UseTestServer();

      builder.Services.AddPingTrace(options =>
      {
        options.Id = Guid.Parse("32DE64E4-6FB4-4F8B-A2C5-C42BF1313141");
        options.Name = "TestApp";
      });

      builder.Services.ConfigureHttpJsonOptions(options =>
      {
        options.SerializerOptions.PropertyNamingPolicy = null;
      });

      if (discovery is not null)
      {
        builder.Services.AddSingleton<IDiscovery>(discovery);
      }

      WebApplication app = builder.Build();

      app.MapPingTrace("/diagnostics");

      if (discovery is not null)
      {
        app.MapPingTraceDiscovery("/diagnostics");
      }

      try
      {
        await app.StartAsync(TestContext.CancellationToken);
        return app;
      }
      catch
      {
        await app.DisposeAsync();
        throw;
      }
    }
  }
}
