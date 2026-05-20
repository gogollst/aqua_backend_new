# Aqua.UserService

User, role, tenant-membership, saved-view and bookmark service for the aqua-backend-ng stack.

## Endpoints

- **Public** (`/api/v1/*`, via `Aqua.ApiGateway` on port 8080):
  - `/api/v1/users/me` — self-service (GET, PATCH, change-password)
  - `/api/v1/users` — admin (List/Get/Create/Patch/Delete/Roles)
  - `/api/v1/roles` — role CRUD
  - `/api/v1/permissions/catalog` — read-only enum export
  - `/api/v1/tenants/{id}/settings` — tenant configuration
  - `/api/v1/ldap/group-mappings` — LDAP-group-to-role mappings
  - `/api/v1/users/me/views`, `/api/v1/users/me/bookmarks` — saved filters + bookmarks
- **Internal** (port 8081, NOT routed through gateway):
  - `GET /internal/v1/users/{id}/claims?tenant={slug}` — claims lookup for SS-05 JWT-issue
  - `POST /internal/v1/ldap/jit-sync` — JIT-provisioning after LDAP-bind
  - `POST /internal/v1/tenants/bootstrap` — idempotent tenant bootstrap

## Running locally

Prerequisites: Docker (Postgres + RabbitMQ), .NET 10 SDK.

```bash
docker compose -f ../../deploy/compose.dev.yml up postgres rabbitmq -d
dotnet run --project src/Aqua.UserService
```

Service listens on `http://localhost:8080` (public, JwtBearer) and `http://localhost:8081` (internal, mTLS or `X-Internal-Token`).

## Tests

```bash
# Unit + integration (uses Testcontainers; Docker required):
dotnet test tests/Aqua.UserService.Tests

# RabbitMQ smoke (opt-in):
dotnet test tests/Aqua.UserService.Tests --filter "Category=BrokerSmoke"

# E2E (requires docker compose stack):
docker compose -f deploy/compose.e2e.yml up -d --build
dotnet test tests/Aqua.UserService.E2E.Tests --filter "Category=E2E"
docker compose -f deploy/compose.e2e.yml down -v
```

## Events published

Topic-exchange `aqua.user-service.events` with routing keys:
- `user.created`, `user.enabled`, `user.disabled`, `user.deleted`, `user.role-changed`, `user.profile-changed`
- `tenant.created`, `tenant.updated`
- `role.created`, `role.updated`, `role.deleted`

Outbox-mediated via `IUserEventPublisher` → `messaging_outbox` table → (future MassTransit relay).

## Schema migrations

`Migrations/*.sql` (applied by SS-09 Migrations-Runner). Additive only:
- `001_add_ldap_dn_column.sql` — `aquauser.ldap_dn` (nullable, indexed)
- `002_add_role_version_column.sql` — `role.version` for optimistic locking
- `003_create_ldap_group_role_mapping.sql` — new mapping table

## Domain

- 7 aggregates: User, Role, Customer + CustomerUserAssignment, LdapGroupRoleMapping, UserView + UserViewFavorite, UserItemBookmark, WelcomePageConfig + UserAssignedProfile
- 24 canonical permissions with dependency closure (see `Roles/Permission.cs` + `PermissionDependencies.cs`)
- Cross-tenant isolation via 3-layer defense: gateway header → middleware → NHibernate filter

## aqua-admin CLI

`tools/aqua-admin/` — operational CLI for tenant bootstrap.

```bash
dotnet run --project tools/aqua-admin -- tenant create \
  --slug acme \
  --display-name "Acme Corp" \
  --admin-email admin@acme.com \
  --internal-token <token>
```

## Known production gaps

1. **Transactional middleware missing.** Outbox events from regular controller actions (e.g. `UserController.Create`) currently flush only when NHibernate happens to need an immediate INSERT (e.g. `native` id-generator). Need a per-request `UnitOfWorkMiddleware` or MVC filter that opens a transaction around each action and commits on success. Tracked as follow-up.
2. **MassTransit broker relay not wired.** `messaging_outbox` rows are written but no background service yet drains them to RabbitMQ. Belongs to SS-09 wave.
3. **`User.CustomerIdHint`** naming inconsistency — should be `CustomerId` matching `ITenantFilteredEntity` convention used by other aggregates. Tracked as cleanup.
4. **Legacy permission-blob format mismatch** — NG uses enum names, legacy aqua DB uses zero-padded numeric IDs (`001010,001050`). ID-mapping table needed for real data migration. See `docs/superpowers/notes/2026-05-20-legacy-permission-blob-format.md` (in aqua-backend repo).
