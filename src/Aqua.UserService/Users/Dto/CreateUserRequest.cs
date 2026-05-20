namespace Aqua.UserService.Users.Dto;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string Surname,
    string? Phone,
    string? Position,
    string? InitialPassword);
