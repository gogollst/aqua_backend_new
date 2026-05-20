using System.Net.Http.Json;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class TenantBootstrapScenario : ScenarioBase
{
    private const string SkipReason = "Requires docker compose -f deploy/compose.e2e.yml up -d";

    [Fact(Skip = SkipReason)]
    public async Task Bootstrap_creates_new_tenant_admin_and_default_roles()
    {
        var resp = await Internal().PostAsJsonAsync("/internal/v1/tenants/bootstrap",
            new BootstrapTenantRequest(
                Slug: "e2e-" + Guid.NewGuid().ToString("n")[..8],
                DisplayName: "E2E Co",
                PrimaryDomain: null,
                Auth: new BootstrapTenantAuth(TenantAuthMode.Local, null, null),
                AdminUser: new BootstrapTenantAdmin(
                    "admin", "admin@e2e.test", "I", "A", "generate", null),
                DefaultRoles: "standard"));

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<BootstrapTenantResponse>();
        body!.InitialPassword.Should().NotBeNullOrEmpty();
        body.RolesCreated.Should().Be(5);
    }
}
