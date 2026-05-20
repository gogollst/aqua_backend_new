using Aqua.UserService.Roles;
using Microsoft.AspNetCore.Authorization;

namespace Aqua.UserService.Infrastructure.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Required { get; }
    public PermissionRequirement(Permission required) => Required = required;
}
