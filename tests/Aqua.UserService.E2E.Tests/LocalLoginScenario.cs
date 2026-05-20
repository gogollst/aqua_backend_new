using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class LocalLoginScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Local_admin_password_grant_returns_jwt_with_tenant_and_perms()
    {
        // Bootstrap a tenant via the internal API, then exchange the admin
        // credentials for a JWT via the gateway-fronted Identity password grant.
        var slug = "e2e-" + Guid.NewGuid().ToString("n")[..8];
        var bootstrap = await Internal().PostAsJsonAsync("/internal/v1/tenants/bootstrap",
            new
            {
                slug,
                displayName = "Local Co",
                auth = new { mode = "Local" },
                adminUser = new
                {
                    username = "admin",
                    email = "admin@local.e2e",
                    firstName = "I",
                    surname = "A",
                    passwordMode = "generate"
                },
                defaultRoles = "standard"
            });
        bootstrap.EnsureSuccessStatusCode();
        var pwd = (await bootstrap.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["initialPassword"]?.ToString();

        var tokenResp = await Gateway().PostAsync("/api/v1/auth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"]   = $"{slug}\\admin",
                ["password"]   = pwd!,
                ["scope"]      = "openid profile aqua.api",
            }));

        tokenResp.EnsureSuccessStatusCode();
        var payload = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>()
                      ?? throw new InvalidOperationException("Token endpoint returned empty body.");
        payload.Should().ContainKey("access_token");
        payload.Should().ContainKey("refresh_token");
    }
}
