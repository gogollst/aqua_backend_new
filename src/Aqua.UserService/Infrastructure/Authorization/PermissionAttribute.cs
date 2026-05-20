using Aqua.UserService.Roles;
using Microsoft.AspNetCore.Authorization;

namespace Aqua.UserService.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(Permission required)
    {
        Policy = $"perm:{(long)required}";
    }

    public static string PolicyFor(Permission p) => $"perm:{(long)p}";
}
