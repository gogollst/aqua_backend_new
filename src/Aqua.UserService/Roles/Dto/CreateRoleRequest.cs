namespace Aqua.UserService.Roles.Dto;

public sealed record CreateRoleRequest(
    string  Name,
    string? Description,
    long    Permissions,
    bool    AvailableInProject,
    bool    AvailableInCustomer,
    bool    IsDefault);
