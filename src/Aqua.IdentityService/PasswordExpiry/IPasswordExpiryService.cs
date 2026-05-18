namespace Aqua.IdentityService.PasswordExpiry;

public interface IPasswordExpiryService
{
    bool IsExpired(DateTime? lastChange);
    DateTime GetExpiryDate(DateTime? lastChange);
}
