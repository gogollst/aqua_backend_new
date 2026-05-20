namespace Aqua.UserService.Events;

public sealed record UserCreated(long UserId, string Username, string Email, bool IsLdap, bool IsFirstAdmin);
