namespace Aqua.UserService.Events;

public sealed record TenantCreated(long TenantId, string Slug, string Name);
