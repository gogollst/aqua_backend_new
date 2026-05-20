namespace Aqua.UserService.Users.Dto;

public sealed record UserDto(
    long   Id,
    string Username,
    string FirstName,
    string Surname,
    string Email,
    string? Phone,
    string? Position,
    UserStatus Status,
    bool ServerAdmin,
    bool Deleted,
    string? LdapDn,
    long Version);
