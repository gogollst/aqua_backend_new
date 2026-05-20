namespace Aqua.UserService.Roles;

public static class PermissionDependencies
{
    private static readonly Dictionary<Permission, Permission[]> _impliesMap = new()
    {
        [Permission.WriteRequirement]    = [Permission.ReadRequirement],
        [Permission.DeleteRequirement]   = [Permission.ReadRequirement],
        [Permission.WriteTestCase]       = [Permission.ReadTestCase],
        [Permission.DeleteTestCase]      = [Permission.ReadTestCase],
        [Permission.ExecuteTest]         = [Permission.ReadTestCase],
        [Permission.WriteDefect]         = [Permission.ReadDefect],
        [Permission.DeleteDefect]        = [Permission.ReadDefect],
        [Permission.ManageUsers]         = [Permission.ReadUser],
        [Permission.ManageRoles]         = [Permission.ReadRole, Permission.ReadUser],
        [Permission.ManageProjects]      = [Permission.ReadProject],
        [Permission.WriteReport]         = [Permission.ReadReport],
        [Permission.ManageWorkflow]      = [Permission.ReadWorkflow],
        [Permission.ManageSyncConfig]    = [Permission.ReadSyncConfig],
        [Permission.ManageTenantSettings] = [Permission.ReadTenantSettings],
    };

    public static Permission Close(Permission input)
    {
        var current = input;
        while (true)
        {
            var next = current;
            foreach (var (perm, implies) in _impliesMap)
            {
                if ((current & perm) == perm)
                {
                    foreach (var implied in implies) next |= implied;
                }
            }
            if (next == current) return current;
            current = next;
        }
    }

    public static (Permission Closure, IReadOnlyList<Permission> Added) CloseWithDiff(Permission input)
    {
        var closure = Close(input);
        var added = new List<Permission>();
        foreach (Permission p in Enum.GetValues<Permission>())
        {
            if (p == Permission.None) continue;
            if ((closure & p) == p && (input & p) != p)
            {
                added.Add(p);
            }
        }
        return (closure, added);
    }

    public static IReadOnlyDictionary<Permission, Permission[]> AsMap() => _impliesMap;
}
