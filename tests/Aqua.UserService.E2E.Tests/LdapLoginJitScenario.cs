using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class LdapLoginJitScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Ldap_token_request_triggers_jit_user_creation()
    {
        // A previously-unseen LDAP user authenticates via the Identity password
        // grant for an LDAP-mode tenant. The Identity service calls the
        // UserService internal LDAP-JIT endpoint, which creates the local user.
        var tokenResp = await Gateway().PostAsync("/api/v1/auth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"]   = "ldap-tenant\\jdoe",
                ["password"]   = "Password1!",
                ["scope"]      = "openid profile aqua.api",
            }));

        tokenResp.EnsureSuccessStatusCode();

        // Verify the user now exists via the internal lookup endpoint.
        var lookup = await Internal().GetAsync("/internal/v1/users/by-username/ldap-tenant/jdoe");
        lookup.EnsureSuccessStatusCode();
        var body = await lookup.Content.ReadFromJsonAsync<Dictionary<string, object>>()
                   ?? throw new InvalidOperationException("Lookup body was empty.");
        body.Should().ContainKey("id");
        body.Should().ContainKey("source");
        body["source"]?.ToString().Should().Be("ldap");
    }
}
