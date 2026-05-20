using Aqua.UserService.Roles.Dto;

namespace Aqua.UserService.Roles;

public static class PermissionCatalog
{
    private static readonly Dictionary<Permission, (string en, string de)> _labels = new()
    {
        [Permission.ReadRequirement]     = ("Read requirements",     "Anforderungen lesen"),
        [Permission.WriteRequirement]    = ("Write requirements",    "Anforderungen bearbeiten"),
        [Permission.DeleteRequirement]   = ("Delete requirements",   "Anforderungen löschen"),
        [Permission.ReadTestCase]        = ("Read test cases",       "Testfälle lesen"),
        [Permission.WriteTestCase]       = ("Write test cases",      "Testfälle bearbeiten"),
        [Permission.DeleteTestCase]      = ("Delete test cases",     "Testfälle löschen"),
        [Permission.ExecuteTest]         = ("Execute tests",         "Tests ausführen"),
        [Permission.ReadDefect]          = ("Read defects",          "Defekte lesen"),
        [Permission.WriteDefect]         = ("Write defects",         "Defekte bearbeiten"),
        [Permission.DeleteDefect]        = ("Delete defects",        "Defekte löschen"),
        [Permission.ReadUser]            = ("Read users",            "Benutzer lesen"),
        [Permission.ManageUsers]         = ("Manage users",          "Benutzer verwalten"),
        [Permission.ReadRole]            = ("Read roles",            "Rollen lesen"),
        [Permission.ManageRoles]         = ("Manage roles",          "Rollen verwalten"),
        [Permission.ReadProject]         = ("Read projects",         "Projekte lesen"),
        [Permission.ManageProjects]      = ("Manage projects",       "Projekte verwalten"),
        [Permission.ReadReport]          = ("Read reports",          "Reports lesen"),
        [Permission.WriteReport]         = ("Write reports",         "Reports bearbeiten"),
        [Permission.ReadWorkflow]        = ("Read workflow",         "Workflow lesen"),
        [Permission.ManageWorkflow]      = ("Manage workflow",       "Workflow verwalten"),
        [Permission.ReadSyncConfig]      = ("Read sync config",      "Sync-Konfiguration lesen"),
        [Permission.ManageSyncConfig]    = ("Manage sync config",    "Sync-Konfiguration verwalten"),
        [Permission.ReadTenantSettings]  = ("Read tenant settings",  "Tenant-Einstellungen lesen"),
        [Permission.ManageTenantSettings] = ("Manage tenant settings", "Tenant-Einstellungen verwalten"),
    };

    public static PermissionCatalogDto Build()
    {
        var implies = PermissionDependencies.AsMap();
        var entries = new List<PermissionCatalogEntry>();
        foreach (Permission p in Enum.GetValues<Permission>())
        {
            if (p == Permission.None) continue;
            var (en, de) = _labels.TryGetValue(p, out var l) ? l : (p.ToString(), p.ToString());
            var impliesList = implies.TryGetValue(p, out var imp)
                ? imp.Select(x => x.ToString()).ToArray()
                : Array.Empty<string>();
            entries.Add(new PermissionCatalogEntry(p.ToString(), (long)p,
                new Dictionary<string, string> { ["en"] = en, ["de"] = de },
                impliesList));
        }
        return new PermissionCatalogDto(entries);
    }
}
