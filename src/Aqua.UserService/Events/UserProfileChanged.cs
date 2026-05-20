namespace Aqua.UserService.Events;

public sealed record UserProfileChanged(long UserId, IReadOnlyList<string> ChangedFields);
