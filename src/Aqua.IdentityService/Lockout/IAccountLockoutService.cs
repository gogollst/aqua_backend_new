namespace Aqua.IdentityService.Lockout;

public interface IAccountLockoutService
{
    bool ShouldLock(int failedAttempts);
    DateTime GetLockoutUntil();
}
