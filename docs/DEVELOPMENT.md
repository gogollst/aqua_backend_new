# Local Development

## Prerequisites
- Docker Desktop (with `docker compose` v2)
- .NET 10 SDK (https://dot.net/download)
- Git

## First-time setup

```bash
git clone <this-repo>
cd aqua-backend-ng
cp .env.example .env             # adjust as needed
dotnet restore
```

## Run the infrastructure stack

```bash
cd deploy
docker compose -f docker-compose.dev.yml up -d
```

Brings up:

| Service   | Local port | Notes                                       |
|---|---|---|
| PostgreSQL| 5432       | user `aqua`, pwd `aqua-dev`, DBs `aqua_acme` and `aqua_default` |
| RabbitMQ  | 5672 / 15672 | Mgmt UI at http://localhost:15672 (aqua / aqua-dev) |
| MinIO     | 9000 / 9001 | Console at http://localhost:9001 (aqua-dev / aqua-dev-secret) |
| Redis     | 6379       | password `aqua-dev`                          |
| Mailhog   | 1025 / 8025 | SMTP at :1025, web UI at http://localhost:8025 |

## Run the tests

```bash
cd ..
dotnet test
```

Some tests use [Testcontainers](https://dotnet.testcontainers.org) and spin up their own ephemeral containers — these run regardless of whether the dev stack is up.

## Tear down

```bash
cd deploy
docker compose -f docker-compose.dev.yml down              # keeps data
docker compose -f docker-compose.dev.yml down -v           # also wipes volumes
```
