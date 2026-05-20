using System.Net;
using System.Net.Http.Json;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users.Dto;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Users;

/// <summary>
/// Integration smoke tests for <see cref="UserSelfController"/>. Drives a real HTTP request
/// through the public branch of the path-based pipeline (TenantContextMiddleware + JwtBearer)
/// and asserts the auth filter / sub-claim resolution land on the right user.
/// </summary>
public sealed class UserSelfControllerTests
    : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly UserServiceWebApplicationFactory _factory;
    public UserSelfControllerTests(UserServiceWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Get_me_without_bearer_returns_401()
    {
        var client = _factory.CreateAnonymous("acme");
        var resp = await client.GetAsync("/api/v1/users/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_me_without_tenant_header_returns_400()
    {
        // Bearer token present, but the TenantContextMiddleware should still reject the
        // missing X-Aqua-Tenant header before auth runs.
        var token = new TestJwtBuilder().ForUser(17L).InTenant("acme", 1L).Build();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/api/v1/users/me");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_me_returns_authenticated_user_dto()
    {
        var (userId, slug, tenantId) = await _factory.SeedAsync(s => s.WithUser("self-alice", "self-acme"));

        var client = _factory.CreateAuthenticated(userId: userId, tenantSlug: slug, tenantId: tenantId);
        var resp = await client.GetAsync("/api/v1/users/me");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<UserDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(userId);
        dto.Username.Should().Be("self-alice");
    }

    [Fact]
    public async Task ChangePassword_returns_501_NotImplemented()
    {
        var (userId, slug, tenantId) = await _factory.SeedAsync(s => s.WithUser("self-pwd", "self-pwd-co"));

        var client = _factory.CreateAuthenticated(userId: userId, tenantSlug: slug, tenantId: tenantId);
        var resp = await client.PostAsJsonAsync("/api/v1/users/me/change-password",
            new { OldPassword = "old", NewPassword = "new" });

        resp.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}
