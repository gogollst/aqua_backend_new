using System.Net;
using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.ApiGateway.Tests.HealthChecks;

public class JwksReachabilityHealthCheckTests
{
    [Fact]
    public async Task Returns_Healthy_when_jwks_endpoint_responds_200()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"keys":[]}""");
        var check = Build(handler, "http://identity:8080");

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Returns_Unhealthy_on_non_2xx()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, "");
        var check = Build(handler, "http://identity:8080");

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Returns_Unhealthy_on_network_error()
    {
        var handler = new ThrowingHandler();
        var check = Build(handler, "http://identity:8080");

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static JwksReachabilityHealthCheck Build(HttpMessageHandler handler, string authority) =>
        new(new HttpClient(handler), Options.Create(new GatewayOptions
        {
            JwtAuthority = authority, JwtAudience = "x",
            Services = new[] { new ServiceConfig { Name = "x", BaseUrl = "http://x", PathPrefix = "/" } },
        }));

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("simulated network error");
    }
}
