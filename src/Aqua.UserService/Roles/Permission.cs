namespace Aqua.UserService.Roles;

[Flags]
public enum Permission : long
{
    None              = 0,
    // --- Requirement aggregate ---
    ReadRequirement   = 1L << 0,
    WriteRequirement  = 1L << 1,
    DeleteRequirement = 1L << 2,
    // --- TestCase aggregate ---
    ReadTestCase      = 1L << 3,
    WriteTestCase     = 1L << 4,
    DeleteTestCase    = 1L << 5,
    ExecuteTest       = 1L << 6,
    // --- Defect aggregate ---
    ReadDefect        = 1L << 7,
    WriteDefect       = 1L << 8,
    DeleteDefect      = 1L << 9,
    // --- Administrative ---
    ReadUser          = 1L << 10,
    ManageUsers       = 1L << 11,
    ReadRole          = 1L << 12,
    ManageRoles       = 1L << 13,
    ReadProject       = 1L << 14,
    ManageProjects    = 1L << 15,
    // --- Reports / dashboards ---
    ReadReport        = 1L << 16,
    WriteReport       = 1L << 17,
    // --- Workflow ---
    ReadWorkflow      = 1L << 18,
    ManageWorkflow    = 1L << 19,
    // --- Integration / sync ---
    ReadSyncConfig    = 1L << 20,
    ManageSyncConfig  = 1L << 21,
    // --- Licensing / tenant ---
    ReadTenantSettings   = 1L << 22,
    ManageTenantSettings = 1L << 23,
}
