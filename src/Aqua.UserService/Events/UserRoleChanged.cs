namespace Aqua.UserService.Events;

public sealed record UserRoleChanged(long UserId, IReadOnlyList<long> OldRoleIds, IReadOnlyList<long> NewRoleIds, string Source);
