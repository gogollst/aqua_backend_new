using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class SelfServiceUpdateScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task User_can_patch_own_profile_and_read_it_back()
    {
        var bearer = "stub-jwt";

        using var gw = Gateway();
        gw.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var patch = await gw.PatchAsJsonAsync("/api/v1/users/me",
            new { firstName = "Renamed", surname = "User" });
        patch.EnsureSuccessStatusCode();

        var get = await gw.GetAsync("/api/v1/users/me");
        get.EnsureSuccessStatusCode();
        var body = await get.Content.ReadFromJsonAsync<Dictionary<string, object>>()
                   ?? throw new InvalidOperationException("GET /users/me body was empty.");
        body["firstName"]?.ToString().Should().Be("Renamed");
        body["surname"]?.ToString().Should().Be("User");
    }
}
