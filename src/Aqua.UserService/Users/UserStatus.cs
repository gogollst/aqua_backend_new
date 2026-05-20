namespace Aqua.UserService.Users;

public enum UserStatus : long
{
    Active   = 0,
    Disabled = 1,
    Locked   = 2,
    Pending  = 3,
}
