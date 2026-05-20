using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class PermissionCatalogExportScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Catalog_endpoint_returns_at_least_24_permissions_with_implies()
    {
        var resp = await Gateway().GetAsync("/api/v1/permissions/catalog");
        resp.EnsureSuccessStatusCode();

        var catalog = await resp.Content.ReadFromJsonAsync<List<CatalogEntry>>();
        catalog.Should().NotBeNull();
        catalog!.Should().HaveCountGreaterThanOrEqualTo(24);
        catalog.Should().Contain(c => c.Implies != null && c.Implies.Count > 0,
            "the implication graph must be populated for at least one permission");
    }

    private sealed record CatalogEntry(string Key, string Description, List<string>? Implies);
}
