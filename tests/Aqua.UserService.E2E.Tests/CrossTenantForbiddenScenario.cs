using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class CrossTenantForbiddenScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task User_in_tenantA_gets_404_when_reading_user_from_tenantB()
    {
        // Two tenants bootstrapped (A, B). Logged-in user in tenant A asks for
        // a user-id known to belong to tenant B → the multi-tenancy filter
        // collapses the read to a 404 (never a 403 — that would leak
        // existence).
        var bearerA       = "stub-jwt-for-tenantA-user";
        var tenantBUserId = 9999L;

        using var gw = Gateway();
        gw.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerA);
        var resp = await gw.GetAsync($"/api/v1/users/{tenantBUserId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
