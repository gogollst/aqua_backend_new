using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Aqua.IdentityService.Domain;

public sealed class AquaUserPasswordMapping : ClassMapping<AquaUserPassword>
{
    public AquaUserPasswordMapping()
    {
        Table("aquaUser");
        Id(x => x.Id, m =>
        {
            m.Column("Id");
            m.Generator(Generators.Native, g => g.Params(new { sequence = "AQUAUSER_ID_SEQ" }));
        });
        Property(x => x.ClearTextPassword,   m => m.Column("ClearTextPassword"));
        Property(x => x.Password,            m => { m.Column("Password"); m.Length(172); });
        Property(x => x.LastPasswordChange,  m => m.Column("LastPasswordChange"));
        Property(x => x.PasswordHistory,     m => { m.Column("PasswordHistory"); m.Type(NHibernateUtil.StringClob); });
        Property(x => x.FailedLoginCount,    m => { m.Column("FailedLoginCount"); m.NotNullable(true); });
        Property(x => x.LockedUntil,         m => m.Column("LockedUntil"));
    }

    public static void Apply(NHibernate.Cfg.Configuration cfg)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<AquaUserPasswordMapping>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
    }
}
