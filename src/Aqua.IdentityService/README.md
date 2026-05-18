# Aqua.IdentityService

JWT-only authentication service for aqua-backend-ng. Issues RSA-256 signed JWTs and rotation-aware refresh tokens.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/auth/token` | Resource-Owner-Password flow → JWT + refresh token |
| POST | `/api/v1/auth/refresh` | Rotate refresh token → new JWT pair |
| GET | `/.well-known/openid-configuration` | OIDC discovery metadata |
| GET | `/.well-known/jwks.json` | Public RSA key for token verification |

## Configuration

See `appsettings.json` for configuration sections:
- `Identity` — RSA key paths, token lifetimes, issuer/audience
- `Ldap` — LDAP/AD server settings (optional)
- `Authentication` — Mode: DatabaseOnly | LdapOnly | BothPreferDatabase | BothPreferLdap
- `AquaData:Tenants` — Per-tenant NHibernate connection strings

## Dev Setup

1. Generate RSA keys: `sh deploy/secrets/generate-dev-keys.sh`
2. Start infrastructure: `docker compose -f deploy/docker-compose.dev.yml up postgres`
3. Run: `dotnet run --project src/Aqua.IdentityService`
