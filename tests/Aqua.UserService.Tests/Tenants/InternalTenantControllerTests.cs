using System.Net;
using System.Net.Http.Json;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Tenants;

/// <summary>
/// Integration smoke tests for <see cref="InternalTenantController"/>. Verifies the InternalApi
/// authentication scheme is wired into the <c>/internal/v1/*</c> branch of the request pipeline
/// (path-based, post Task 33 refactor) and that a happy-path bootstrap call returns 201 Created.
/// </summary>
public sealed class InternalTenantControllerTests
    : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly UserServiceWebApplicationFactory _factory;
    public InternalTenantControllerTests(UserServiceWebApplicationFactory f) => _factory = f;

    private static BootstrapTenantRequest StandardRequest(string slug, string adminUser) => new(
        Slug: slug,
        DisplayName: "Smoke Co",
        PrimaryDomain: null,
        Auth: new BootstrapTenantAuth(TenantAuthMode.Local, null, null),
        AdminUser: new BootstrapTenantAdmin(
            Username: adminUser,
            Email: $"{adminUser}@example.com",
            FirstName: "Smoke",
            Surname: "Tester",
            PasswordMode: "generate",
            Password: null),
        DefaultRoles: "minimal");

    [Fact]
    public async Task Bootstrap_returns_201_with_valid_internal_token()
    {
        var slug = ("smk-" + Guid.NewGuid().ToString("N"))[..16];
        var client = _factory.CreateInternal();

        var resp = await client.PostAsJsonAsync("/internal/v1/tenants/bootstrap",
            StandardRequest(slug, $"adm-{slug}"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<BootstrapTenantResponse>();
        body.Should().NotBeNull();
        body!.TenantSlug.Should().Be(slug);
        body.Skipped.Should().BeFalse();
        body.InitialPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Bootstrap_returns_401_without_internal_token()
    {
        // CreateClient (not CreateInternal) — no X-Internal-Token header attached.
        var client = _factory.CreateClient();
        var slug = ("smk-" + Guid.NewGuid().ToString("N"))[..16];

        var resp = await client.PostAsJsonAsync("/internal/v1/tenants/bootstrap",
            StandardRequest(slug, $"adm-{slug}"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bootstrap_returns_401_with_wrong_internal_token()
    {
        var client = _factory.CreateInternal(token: "wrong-token");
        var slug = ("smk-" + Guid.NewGuid().ToString("N"))[..16];

        var resp = await client.PostAsJsonAsync("/internal/v1/tenants/bootstrap",
            StandardRequest(slug, $"adm-{slug}"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
