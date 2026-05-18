-- Database `aqua` is auto-created by POSTGRES_DB. Create a separate database for each tenant in dev mode.
CREATE DATABASE aqua_acme;
CREATE DATABASE aqua_default;

\c aqua_acme;
CREATE TABLE messaging_outbox (
  id            UUID PRIMARY KEY,
  tenant_id     VARCHAR(64) NOT NULL,
  message_type  VARCHAR(256) NOT NULL,
  payload       TEXT NOT NULL,
  headers_json  TEXT NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL,
  dispatched_at TIMESTAMPTZ NULL,
  attempts      INT NOT NULL,
  last_error    VARCHAR(2000) NULL
);
CREATE INDEX ix_outbox_pending ON messaging_outbox (created_at) WHERE dispatched_at IS NULL;

CREATE TABLE messaging_inbox (
  id            UUID NOT NULL,
  consumer      VARCHAR(256) NOT NULL,
  tenant_id     VARCHAR(64) NOT NULL,
  processed_at  TIMESTAMPTZ NOT NULL,
  PRIMARY KEY (id, consumer)
);

CREATE TABLE auth_refresh_token (
  id                   UUID PRIMARY KEY,
  user_id              INT NOT NULL,
  tenant_id            VARCHAR(64) NOT NULL,
  token_hash           VARCHAR(128) NOT NULL UNIQUE,
  issued_at            TIMESTAMPTZ NOT NULL,
  expires_at           TIMESTAMPTZ NOT NULL,
  rotated_to_token_id  UUID NULL,
  revoked_at           TIMESTAMPTZ NULL,
  revocation_reason    VARCHAR(200) NULL,
  client_ip            VARCHAR(64) NULL
);
CREATE INDEX ix_refresh_token_user ON auth_refresh_token (user_id);
CREATE INDEX ix_refresh_token_active ON auth_refresh_token (token_hash) WHERE revoked_at IS NULL AND rotated_to_token_id IS NULL;

\c aqua_default;
CREATE TABLE messaging_outbox (
  id            UUID PRIMARY KEY,
  tenant_id     VARCHAR(64) NOT NULL,
  message_type  VARCHAR(256) NOT NULL,
  payload       TEXT NOT NULL,
  headers_json  TEXT NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL,
  dispatched_at TIMESTAMPTZ NULL,
  attempts      INT NOT NULL,
  last_error    VARCHAR(2000) NULL
);
CREATE INDEX ix_outbox_pending ON messaging_outbox (created_at) WHERE dispatched_at IS NULL;

CREATE TABLE messaging_inbox (
  id            UUID NOT NULL,
  consumer      VARCHAR(256) NOT NULL,
  tenant_id     VARCHAR(64) NOT NULL,
  processed_at  TIMESTAMPTZ NOT NULL,
  PRIMARY KEY (id, consumer)
);

CREATE TABLE auth_refresh_token (
  id                   UUID PRIMARY KEY,
  user_id              INT NOT NULL,
  tenant_id            VARCHAR(64) NOT NULL,
  token_hash           VARCHAR(128) NOT NULL UNIQUE,
  issued_at            TIMESTAMPTZ NOT NULL,
  expires_at           TIMESTAMPTZ NOT NULL,
  rotated_to_token_id  UUID NULL,
  revoked_at           TIMESTAMPTZ NULL,
  revocation_reason    VARCHAR(200) NULL,
  client_ip            VARCHAR(64) NULL
);
CREATE INDEX ix_refresh_token_user ON auth_refresh_token (user_id);
CREATE INDEX ix_refresh_token_active ON auth_refresh_token (token_hash) WHERE revoked_at IS NULL AND rotated_to_token_id IS NULL;

-- Seed data for smoke-tests (intentionally minimal; per-service tables are created in P1+).
\c aqua_acme;
-- One known UUID so smoke-tests can assert a successful no-op publish.
INSERT INTO messaging_outbox (id, tenant_id, message_type, payload, headers_json, created_at, dispatched_at, attempts)
VALUES ('00000000-0000-0000-0000-000000000001', 'acme', 'Aqua.Contracts.Events.HealthCheckEvent',
        '{"MessageId":"00000000-0000-0000-0000-000000000001"}', '{}', NOW(), NOW(), 0);
