namespace Aqua.UserService.Roles.Dto;

public sealed record PermissionCatalogDto(IReadOnlyList<PermissionCatalogEntry> Entries);

public sealed record PermissionCatalogEntry(
    string Key,
    long   Bit,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<string> Implies);
