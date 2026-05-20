using Aqua.UserService.Roles;
using Microsoft.AspNetCore.Authorization;

namespace Aqua.UserService.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("serveradmin", "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        var permsClaim = context.User.FindFirst("perms")?.Value;
        if (long.TryParse(permsClaim, out var bits))
        {
            var actual = (Permission)bits;
            if ((actual & requirement.Required) == requirement.Required)
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }
}
