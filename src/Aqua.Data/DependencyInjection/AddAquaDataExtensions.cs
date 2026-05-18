using Aqua.Data.Inbox;
using Aqua.Data.Outbox;
using Aqua.Data.Sessions;
using Aqua.Data.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using NHibernate.Cfg;

namespace Aqua.Data.DependencyInjection;

public static class AddAquaDataExtensions
{
    /// <summary>
    /// Registers NHibernate (multi-tenant) + Outbox/Inbox infrastructure.
    /// Call once per service in <c>builder.Services</c>.
    /// </summary>
    public static IServiceCollection AddAquaData(
        this IServiceCollection services,
        Action<AquaDataOptions> configure,
        params Action<Configuration>[] mappings)
    {
        var opts = new AquaDataOptions();
        configure(opts);

        var allMappings = new List<Action<Configuration>>(mappings)
        {
            OutboxMessageMapping.Apply,
            InboxMessageMapping.Apply,
        };

        services.AddSingleton(opts);
        services.AddSingleton(sp => new SessionFactoryRegistry(opts, allMappings));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ISessionScope, SessionScope>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        return services;
    }
}
