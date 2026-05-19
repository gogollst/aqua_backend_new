# Aqua.ApiGateway

YARP-based reverse-proxy / API-Gateway for the aqua-backend-ng stack.

## Local run

The gateway is part of the `deploy/docker-compose.dev.yml` stack. To start everything:

```bash
cd deploy
docker compose up --build
```

- Gateway is exposed on `http://localhost:8080` (the only exposed port — `identity-service` is now internal).
- Login: `POST http://localhost:8080/api/v1/auth/token` with `{ "grant_type": "password", "username": "admin", "password": "admin" }`.
- Health: `GET http://localhost:8080/healthz` (liveness) and `GET http://localhost:8080/readyz` (readiness).
- Aggregated OpenAPI: `GET http://localhost:8080/openapi.json`.

## Configuration

All settings under `Gateway`, `TenantResolution`, `RateLimits`, `Resilience` in `appsettings.json`,
overridable via environment variables using the standard `__`-separator convention:

```bash
Gateway__Services__0__BaseUrl=http://identity-service:8080
TenantResolution__Mode=Subdomain
RateLimits__PerIp__PermitLimit=200
```

## Adding a downstream service

Append to `Gateway.Services` in `appsettings.{Environment}.json`:

```json
{
  "Name": "users",
  "BaseUrl": "http://user-service:8080",
  "PathPrefix": "/api/v1/users"
}
```

Restart the gateway. No code change required.

## Tests

```bash
dotnet test tests/Aqua.ApiGateway.Tests
```

The `FullStackGatewayIdentityTest` is Docker-gated: it skips when Docker isn't running.
