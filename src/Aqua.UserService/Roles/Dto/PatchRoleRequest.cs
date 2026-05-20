namespace Aqua.UserService.Roles.Dto;

public sealed record PatchRoleRequest(
    string? Name,
    string? Description,
    long?   Permissions,
    bool?   AvailableInProject,
    bool?   AvailableInCustomer,
    bool?   IsDefault,
    long    Version);
