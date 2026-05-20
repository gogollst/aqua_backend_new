namespace Aqua.UserService.Users.Dto;

public sealed record PatchUserRequest(
    string? FirstName,
    string? Surname,
    string? Email,
    string? Phone,
    string? Position,
    long    Version);
