namespace Aqua.UserService.Events;

public sealed record TenantUpdated(long TenantId, IReadOnlyList<string> ChangedFields);
