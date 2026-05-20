using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class StaleVersionConflictScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Stale_version_patch_returns_409_with_current_version()
    {
        var bearer = "stub-jwt";

        using var gw = Gateway();
        gw.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var first = await gw.PatchAsJsonAsync("/api/v1/users/me",
            new { firstName = "First", expectedVersion = 1 });
        first.EnsureSuccessStatusCode();

        // Second PATCH with stale version – server replies 409.
        var conflict = await gw.PatchAsJsonAsync("/api/v1/users/me",
            new { firstName = "Second", expectedVersion = 1 });

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await conflict.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!.Should().ContainKey("currentVersion");
    }
}
