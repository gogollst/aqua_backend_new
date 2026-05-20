namespace Aqua.UserService.Roles.Dto;

public sealed record RoleDto(
    long    Id,
    string  Name,
    string? Description,
    long    CustomerId,
    bool    AvailableInProject,
    bool    AvailableInCustomer,
    bool    IsDefault,
    long    Permissions,
    string? PermVersion,
    long    Version);

public sealed record RoleMutationWarnings(IReadOnlyList<string> AdjustedPermissions);
