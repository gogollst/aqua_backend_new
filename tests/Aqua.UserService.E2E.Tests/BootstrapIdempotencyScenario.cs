using System.Net.Http.Json;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class BootstrapIdempotencyScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Calling_bootstrap_twice_with_same_slug_returns_skipped_true()
    {
        var slug = "e2e-idem-" + Guid.NewGuid().ToString("n")[..8];

        var request = new BootstrapTenantRequest(
            Slug: slug,
            DisplayName: "Idem Co",
            PrimaryDomain: null,
            Auth: new BootstrapTenantAuth(TenantAuthMode.Local, null, null),
            AdminUser: new BootstrapTenantAdmin(
                "admin", "admin@idem.e2e", "I", "A", "generate", null),
            DefaultRoles: "standard");

        var first  = await Internal().PostAsJsonAsync("/internal/v1/tenants/bootstrap", request);
        first.EnsureSuccessStatusCode();
        var firstBody = await first.Content.ReadFromJsonAsync<BootstrapTenantResponse>()
                        ?? throw new InvalidOperationException("First bootstrap response was empty.");
        firstBody.Skipped.Should().BeFalse();

        var second = await Internal().PostAsJsonAsync("/internal/v1/tenants/bootstrap", request);
        second.EnsureSuccessStatusCode();
        var secondBody = await second.Content.ReadFromJsonAsync<BootstrapTenantResponse>()
                         ?? throw new InvalidOperationException("Second bootstrap response was empty.");
        secondBody.Skipped.Should().BeTrue();
        secondBody.TenantId.Should().Be(firstBody.TenantId);
    }
}
