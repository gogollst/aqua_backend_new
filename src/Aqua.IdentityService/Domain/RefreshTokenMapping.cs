using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Aqua.IdentityService.Domain;

public sealed class RefreshTokenMapping : ClassMapping<RefreshToken>
{
    public RefreshTokenMapping()
    {
        Table("auth_refresh_token");
        Id(x => x.Id, m => { m.Column("id"); m.Generator(Generators.Assigned); });
        Property(x => x.UserId,            m => { m.Column("user_id"); m.NotNullable(true); });
        Property(x => x.TenantId,          m => { m.Column("tenant_id"); m.Length(64); m.NotNullable(true); });
        Property(x => x.TokenHash,         m => { m.Column("token_hash"); m.Length(128); m.NotNullable(true); });
        Property(x => x.IssuedAt,          m => { m.Column("issued_at"); m.NotNullable(true); });
        Property(x => x.ExpiresAt,         m => { m.Column("expires_at"); m.NotNullable(true); });
        Property(x => x.RotatedToTokenId,  m => m.Column("rotated_to_token_id"));
        Property(x => x.RevokedAt,         m => m.Column("revoked_at"));
        Property(x => x.RevocationReason,  m => { m.Column("revocation_reason"); m.Length(200); });
        Property(x => x.ClientIp,          m => { m.Column("client_ip"); m.Length(64); });
    }

    public static void Apply(NHibernate.Cfg.Configuration cfg)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<RefreshTokenMapping>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
    }
}
