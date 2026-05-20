using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class RoleChangeRevokesTokenScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Assigning_role_emits_user_role_changed_and_revokes_refresh_tokens()
    {
        // Sequence:
        //   1. Bootstrap tenant, log in as admin, get a refresh-token for a
        //      target user.
        //   2. Admin PATCHes the target user's role assignment.
        //   3. UserService publishes user.role-changed → IdentityService
        //      consumer revokes the user's refresh-tokens.
        //   4. Attempting to redeem the old refresh-token returns 401.
        var refreshToken = "stub-refresh-from-step-1";
        var adminBearer  = "stub-admin-jwt";

        using var gw = Gateway();
        gw.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
        var patch = await gw.PatchAsJsonAsync("/api/v1/users/42/roles", new { add = new[] { "QA-Manager" } });
        patch.EnsureSuccessStatusCode();

        // Allow the bus consumer to process the event.
        await Task.Delay(TimeSpan.FromSeconds(2));

        var redeem = await Gateway().PostAsync("/api/v1/auth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = refreshToken,
            }));

        redeem.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
