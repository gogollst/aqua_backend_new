using System.Security.Claims;
using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Roles;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler = new();

    [Fact]
    public async Task Succeeds_when_perms_claim_has_bit()
    {
        var ctx = MakeContext(Permission.ManageUsers, userPermsBitset: (long)Permission.ManageUsers);
        await _handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_when_perms_claim_missing_bit()
    {
        var ctx = MakeContext(Permission.ManageRoles, userPermsBitset: (long)Permission.ManageUsers);
        await _handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Succeeds_when_user_is_server_admin()
    {
        var ctx = MakeContext(Permission.ManageUsers, userPermsBitset: 0, serverAdmin: true);
        await _handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    private static AuthorizationHandlerContext MakeContext(Permission required, long userPermsBitset, bool serverAdmin = false)
    {
        var claims = new List<Claim> { new("perms", userPermsBitset.ToString()) };
        if (serverAdmin) claims.Add(new("serveradmin", "true"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
        var req = new PermissionRequirement(required);
        return new AuthorizationHandlerContext(new[] { req }, principal, resource: null);
    }
}
