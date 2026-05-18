using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Aqua.Data.Outbox;

public sealed class OutboxMessageMapping : ClassMapping<OutboxMessage>
{
    public OutboxMessageMapping()
    {
        Table("messaging_outbox");
        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.Assigned);
        });
        // Explicit snake_case column names to match the DDL in Task 17 / Task 18.
        // By default NHibernate uses PascalCase property names as column names; here we override.
        Property(x => x.TenantId, m =>
        {
            m.Column("tenant_id");
            m.Length(64);
            m.NotNullable(true);
        });
        Property(x => x.MessageType, m =>
        {
            m.Column("message_type");
            m.Length(256);
            m.NotNullable(true);
        });
        Property(x => x.Payload, m =>
        {
            m.Column("payload");
            m.Type(NHibernateUtil.StringClob);
            m.NotNullable(true);
        });
        Property(x => x.HeadersJson, m =>
        {
            m.Column("headers_json");
            m.Type(NHibernateUtil.StringClob);
            m.NotNullable(true);
        });
        Property(x => x.CreatedAt, m =>
        {
            m.Column("created_at");
            m.NotNullable(true);
        });
        Property(x => x.DispatchedAt, m => m.Column("dispatched_at"));
        Property(x => x.Attempts, m =>
        {
            m.Column("attempts");
            m.NotNullable(true);
        });
        Property(x => x.LastError, m =>
        {
            m.Column("last_error");
            m.Length(2000);
        });
    }

    public static void Apply(Configuration cfg)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<OutboxMessageMapping>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
    }
}
