namespace Aqua.UserService.Events;

public sealed record UserDisabled(long UserId, string Reason);
