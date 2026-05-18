namespace Aqua.IdentityService.PasswordExpiry;

public sealed class PasswordExpiryService : IPasswordExpiryService
{
    public static readonly TimeSpan ExpiryWindow = TimeSpan.FromDays(90);

    public bool IsExpired(DateTime? lastChange)
    {
        if (lastChange is null) return true;
        return DateTime.UtcNow - lastChange.Value > ExpiryWindow;
    }

    public DateTime GetExpiryDate(DateTime? lastChange) =>
        (lastChange ?? DateTime.UtcNow).Add(ExpiryWindow);
}
