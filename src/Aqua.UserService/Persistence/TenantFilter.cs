using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Persistence;

public static class TenantFilter
{
    public const string Name = "tenant_filter";
    public const string TenantIdParam = "tenantId";
    public const string Condition = "customer_id = :tenantId";

    public static void Register(Configuration cfg)
    {
        cfg.AddFilterDefinition(new NHibernate.Engine.FilterDefinition(
            Name,
            Condition,
            new Dictionary<string, NHibernate.Type.IType>
            {
                [TenantIdParam] = NHibernateUtil.Int64
            },
            useManyToOne: false));
    }

    public static ISession EnableFor(ISession session, long tenantId)
    {
        var filter = session.EnableFilter(Name);
        filter.SetParameter(TenantIdParam, tenantId);
        return session;
    }
}
