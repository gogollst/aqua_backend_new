using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class JitZeroGroupsRejectedScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Ldap_user_with_no_mapped_groups_is_rejected_with_invalid_grant()
    {
        // The LDAP-tenant has group mappings configured, but the test user is
        // in no mapped group. JIT-sync rejects the auth-attempt; Identity must
        // surface OAuth 401 invalid_grant.
        var resp = await Gateway().PostAsync("/api/v1/auth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"]   = "ldap-tenant\\orphan-user",
                ["password"]   = "Password1!",
                ["scope"]      = "openid profile aqua.api",
            }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>()
                   ?? throw new InvalidOperationException("Token endpoint returned empty body.");
        body["error"]?.ToString().Should().Be("invalid_grant");
    }
}
