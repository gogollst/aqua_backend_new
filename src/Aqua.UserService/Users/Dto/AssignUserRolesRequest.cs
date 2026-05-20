namespace Aqua.UserService.Users.Dto;

/// <summary>
/// Body of PUT /api/v1/users/{id}/roles — replaces the user's role set for the current
/// tenant with the supplied ids (set semantics, not delta).
/// </summary>
public sealed record AssignUserRolesRequest(IReadOnlyList<long> RoleIds);
