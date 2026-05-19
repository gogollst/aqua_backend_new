using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace Aqua.ApiGateway.Tests.Integration;

public class EndToEndRoutingTests : IAsyncLifetime
{
    private MockBackendFixture _backend = default!;
    private GatewayWebApplicationFactory _gateway = default!;
    private HttpClient _client = default!;

    public Task InitializeAsync()
    {
        _backend = new MockBackendFixture();
        _gateway = new GatewayWebApplicationFactory
        {
            IdentityBaseUrl = _backend.BaseUrl,
            JwtAuthority    = _backend.BaseUrl,
        };
        // Stub the OIDC discovery + JWKS for JwtBearer's ConfigurationManager.
        _backend.SetResponse("/.well-known/openid-configuration", 200, $"{{\"issuer\":\"{_backend.BaseUrl}\",\"jwks_uri\":\"{_backend.BaseUrl}/.well-known/jwks.json\"}}");
        _backend.SetResponse("/.well-known/jwks.json", 200, "{\"keys\":[]}");
        _client = _gateway.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _gateway.DisposeAsync();
        await _backend.DisposeAsync();
    }

    [Fact]
    public async Task Anonymous_token_endpoint_is_forwarded()
    {
        _backend.SetResponse("/api/v1/auth/token", 200, "{\"access_token\":\"x\"}");

        var response = await _client.PostAsync("/api/v1/auth/token", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _backend.ReceivedRequests.Should().Contain(r => r.Path == "/api/v1/auth/token");
    }

    [Fact]
    public async Task Authenticated_route_without_jwt_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/users/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Per_ip_rate_limit_returns_429_when_exceeded()
    {
        // PerIp default is 100/10s in test config — we'll send 101 in a tight loop.
        // Use a single client (same connection) so RemoteIp is consistent.
        for (var i = 0; i < 100; i++)
            await _client.PostAsync("/api/v1/auth/token", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        var rejected = await _client.PostAsync("/api/v1/auth/token", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task Tenant_header_propagated_to_backend()
    {
        _backend.SetResponse("/api/v1/auth/token", 200, "{}");
        await _client.PostAsync("/api/v1/auth/token", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        var lastReq = _backend.ReceivedRequests.Last();
        lastReq.Headers.Should().ContainKey("X-Aqua-Tenant");
        lastReq.Headers["X-Aqua-Tenant"].Should().Be("default");
    }

    [Fact]
    public async Task Correlation_id_is_set_and_propagated()
    {
        _backend.SetResponse("/api/v1/auth/token", 200, "{}");
        await _client.PostAsync("/api/v1/auth/token", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        var lastReq = _backend.ReceivedRequests.Last();
        lastReq.Headers.Should().ContainKey("X-Correlation-Id");
        Guid.TryParse(lastReq.Headers["X-Correlation-Id"], out _).Should().BeTrue();
    }

    [Fact]
    public async Task Custom_header_is_stripped_by_whitelist()
    {
        _backend.SetResponse("/api/v1/auth/token", 200, "{}");
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("X-Custom-Debug", "should-be-stripped");
        await _client.SendAsync(req);

        var lastReq = _backend.ReceivedRequests.Last();
        lastReq.Headers.Should().NotContainKey("X-Custom-Debug");
    }

    [Fact]
    public async Task Healthz_returns_200()
    {
        var response = await _client.GetAsync("/healthz");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
