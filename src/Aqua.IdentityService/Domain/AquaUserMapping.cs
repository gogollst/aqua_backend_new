using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Aqua.IdentityService.Domain;

public sealed class AquaUserMapping : ClassMapping<AquaUser>
{
    public AquaUserMapping()
    {
        Table("aquaUser");
        Id(x => x.Id, m =>
        {
            m.Column("Id");
            m.Generator(Generators.Native, g => g.Params(new { sequence = "AQUAUSER_ID_SEQ" }));
        });
        Property(x => x.UserName, m => { m.Column("UserName"); m.Length(50); m.NotNullable(true); m.Unique(true); });
        Property(x => x.FirstName, m => { m.Column("FirstName"); m.Length(100); });
        Property(x => x.Surname,   m => { m.Column("Surname"); m.Length(100); });
        Property(x => x.Email,     m => { m.Column("Email"); m.Length(100); });
        Property(x => x.LastLogin, m => m.Column("LastLogin"));
        Property(x => x.Phone,     m => { m.Column("Phone"); m.Length(50); });
        Property(x => x.Position,  m => { m.Column("Position"); m.Length(100); });
        Property(x => x.UserStatus,            m => m.Column("UserStatus"));
        Property(x => x.Deleted,               m => m.Column("Deleted"));
        Property(x => x.ServerAdmin,           m => m.Column("ServerAdmin"));
        Property(x => x.PasswordExpiryExempt,  m => m.Column("PasswordExpiryExempt"));
    }

    public static void Apply(NHibernate.Cfg.Configuration cfg)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<AquaUserMapping>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
    }
}
