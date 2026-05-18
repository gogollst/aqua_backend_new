namespace Aqua.IdentityService.Lockout;

public sealed class AccountLockoutService : IAccountLockoutService
{
    /// <summary>Mirror of legacy threshold (RQ066560).</summary>
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public bool ShouldLock(int failedAttempts) => failedAttempts >= MaxFailedAttempts;

    public DateTime GetLockoutUntil() => DateTime.UtcNow.Add(LockoutDuration);
}
