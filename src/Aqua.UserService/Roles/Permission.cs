namespace Aqua.UserService.Roles;

[Flags]
public enum Permission : long
{
    None        = 0,
    ManageUsers = 1L << 10,
    ManageRoles = 1L << 11,
    // Full canonical set added in Task 8.
}
