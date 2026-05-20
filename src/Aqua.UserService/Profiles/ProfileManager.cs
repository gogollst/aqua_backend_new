using Aqua.UserService.Domain;
using Aqua.UserService.Profiles.Dto;
using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Profiles;

public interface IProfileManager
{
    Task<WelcomePageConfigDto> GetWelcomeConfigAsync(long userId, long customerId);
    Task<WelcomePageConfigDto> PutWelcomeConfigAsync(long userId, PutWelcomePageConfigRequest req, long customerId);
}

public sealed class ProfileManager : IProfileManager
{
    private readonly ISession _session;
    public ProfileManager(ISession session) => _session = session;

    public async Task<WelcomePageConfigDto> GetWelcomeConfigAsync(long userId, long customerId)
    {
        var existing = await _session.Query<WelcomePageConfig>()
            .FirstOrDefaultAsync(w => w.UserId == userId);
        if (existing is null)
        {
            existing = new WelcomePageConfig { CustomerId = customerId, UserId = userId, ConfigJson = "{}" };
            await _session.SaveAsync(existing);
        }
        return new WelcomePageConfigDto(existing.Id, existing.UserId, existing.ConfigJson, existing.Version);
    }

    public async Task<WelcomePageConfigDto> PutWelcomeConfigAsync(long userId, PutWelcomePageConfigRequest req, long customerId)
    {
        var existing = await _session.Query<WelcomePageConfig>()
            .FirstOrDefaultAsync(w => w.UserId == userId);
        if (existing is null)
        {
            existing = new WelcomePageConfig { CustomerId = customerId, UserId = userId, ConfigJson = req.ConfigJson };
            await _session.SaveAsync(existing);
        }
        else
        {
            if (existing.Version != req.Version)
                throw new StaleVersionException(existing.Version, "WelcomeConfig version stale.");
            existing.ConfigJson = req.ConfigJson;
        }
        return new WelcomePageConfigDto(existing.Id, existing.UserId, existing.ConfigJson, existing.Version);
    }
}
