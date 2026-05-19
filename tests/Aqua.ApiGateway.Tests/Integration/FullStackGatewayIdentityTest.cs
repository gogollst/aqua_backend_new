using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.ApiGateway.Tests.Integration;

public sealed class FullStackGatewayIdentityTest
{
    private const string DockerNotAvailable = "Docker is not running locally — skipping container-based end-to-end test.";

    private static bool IsDockerRunning()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "version") { RedirectStandardOutput = true, RedirectStandardError = true };
            var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(2_000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    [Fact(Skip = DockerNotAvailable)]
    public async Task Login_then_refresh_via_gateway_roundtrip()
    {
        if (!IsDockerRunning()) return;

        using var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            grant_type = "password",
            username   = "admin",
            password   = "admin",
        });
        loginResponse.IsSuccessStatusCode.Should().BeTrue($"login should succeed; body: {await loginResponse.Content.ReadAsStringAsync()}");
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokens!.access_token.Should().NotBeNullOrEmpty();
        tokens.refresh_token.Should().NotBeNullOrEmpty();

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refresh_token = tokens.refresh_token });
        refreshResponse.IsSuccessStatusCode.Should().BeTrue();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        refreshed!.access_token.Should().NotBeNullOrEmpty();
        refreshed.access_token.Should().NotBe(tokens.access_token);
    }

    private sealed record TokenResponse(string access_token, string refresh_token, int expires_in);
}
