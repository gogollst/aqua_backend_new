# Configuration Templates

`appsettings.Development.template.json` is the canonical example wired against the local
docker-compose dev-stack. When you scaffold a new service, copy this file to
`src/Aqua.<Service>/appsettings.Development.json` and set:

1. `Otel.ServiceName` to the service name (e.g. `"identity-service"`).
2. Service-specific section under the service's own key (e.g. `"IdentityService": { ... }`).

The file should NEVER be committed with real secrets. The dev-stack values here are intentionally
shared so developers don't need to coordinate on credentials.
