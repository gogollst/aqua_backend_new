using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Aqua.Data.Inbox;

public sealed class InboxMessageMapping : ClassMapping<InboxMessage>
{
    public InboxMessageMapping()
    {
        Table("messaging_inbox");
        // Composite PK: (id, consumer) — matching DDL: PRIMARY KEY (id, consumer)
        ComposedId(c =>
        {
            c.Property(x => x.Id, m => m.Column("id"));
            c.Property(x => x.Consumer, m =>
            {
                m.Column("consumer");
                m.Length(256);
            });
        });
        Property(x => x.TenantId, m =>
        {
            m.Column("tenant_id");
            m.Length(64);
            m.NotNullable(true);
        });
        Property(x => x.ProcessedAt, m =>
        {
            m.Column("processed_at");
            m.NotNullable(true);
        });
    }

    public static void Apply(Configuration cfg)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<InboxMessageMapping>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
    }
}
