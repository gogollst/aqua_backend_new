namespace Aqua.UserService.Roles;

public sealed class PermissionBitset : IEquatable<PermissionBitset>
{
    public Permission Flags { get; }
    public IReadOnlyList<string> UnknownTokens { get; }

    public static PermissionBitset None { get; } = new(Permission.None, Array.Empty<string>());

    private PermissionBitset(Permission flags, IReadOnlyList<string> unknown)
    {
        Flags = flags;
        UnknownTokens = unknown;
    }

    public static PermissionBitset From(Permission flags) => new(flags, Array.Empty<string>());

    public static PermissionBitset FromLegacyBlob(string? blob)
    {
        if (string.IsNullOrWhiteSpace(blob)) return None;

        var tokens = blob.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var flags = Permission.None;
        var unknown = new List<string>();

        foreach (var token in tokens)
        {
            if (Enum.TryParse<Permission>(token, ignoreCase: true, out var parsed) &&
                parsed != Permission.None)
            {
                flags |= parsed;
            }
            else
            {
                unknown.Add(token);
            }
        }
        return new PermissionBitset(flags, unknown);
    }

    public string ToLegacyBlob()
    {
        if (Flags == Permission.None) return "";
        var ordered = Enum.GetValues<Permission>()
            .Where(p => p != Permission.None && (Flags & p) == p)
            .OrderBy(p => (long)p)
            .Select(p => p.ToString());
        return string.Join(",", ordered);
    }

    public bool Has(Permission p) => (Flags & p) == p && p != Permission.None;

    public (PermissionBitset Closure, IReadOnlyList<Permission> Added) EnforceDependencies()
    {
        var (closed, added) = PermissionDependencies.CloseWithDiff(Flags);
        return (new PermissionBitset(closed, UnknownTokens), added);
    }

    public bool Equals(PermissionBitset? other) => other is not null && other.Flags == Flags;
    public override bool Equals(object? obj) => obj is PermissionBitset pb && Equals(pb);
    public override int GetHashCode() => Flags.GetHashCode();
    public override string ToString() => ToLegacyBlob();
}
