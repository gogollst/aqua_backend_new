# aqua-backend-ng

Greenfield rewrite of the aqua-backend on .NET 10 / Linux / Microservices.

See: master design spec at `aqua-backend/docs/superpowers/specs/2026-05-18-aqua-backend-modernisierung-design.md` (in the legacy repo).

## Quick start

```bash
# Install .NET 10 SDK first.
dotnet restore
dotnet build
dotnet test
```

See `docs/DEVELOPMENT.md` for local-dev-stack setup (docker-compose with PostgreSQL/RabbitMQ/MinIO/Redis/Mailhog).
