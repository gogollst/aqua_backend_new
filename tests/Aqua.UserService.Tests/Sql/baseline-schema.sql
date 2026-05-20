-- Baseline schema for integration tests.
-- This bootstraps the minimum tables needed by tests in tasks 11-17.
-- Task 18 will replace this with the full schema migration script.

CREATE TABLE IF NOT EXISTS aquauser (
    id                    bigserial PRIMARY KEY,
    username              varchar(255) NOT NULL,
    firstname             varchar(255),
    surname               varchar(255),
    email                 varchar(255),
    phone                 varchar(255),
    position              varchar(255),
    picturescreenshotid   bigint,
    status                bigint NOT NULL DEFAULT 0,
    serveradmin           boolean NOT NULL DEFAULT false,
    deleted               boolean NOT NULL DEFAULT false,
    lastloginunixseconds  bigint,
    userdata              text,
    userdataweb           text,
    ldap_dn               varchar(512),
    customer_id           bigint NOT NULL,
    version               bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS role (
    id                   bigserial PRIMARY KEY,
    name                 varchar(255) NOT NULL,
    description          varchar(1024),
    customer_id          bigint NOT NULL,
    availableinproject   boolean NOT NULL DEFAULT true,
    availableincustomer  boolean NOT NULL DEFAULT true,
    isdefault            boolean NOT NULL DEFAULT false,
    permissions          text,
    permversion          varchar(64),
    version              bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS customer (
    id              bigserial PRIMARY KEY,
    slug            varchar(64) NOT NULL UNIQUE,
    displayname     varchar(255) NOT NULL,
    primarydomain   varchar(255) NULL,
    authmode        bigint NOT NULL DEFAULT 0,
    authconfigjson  text NULL,
    version         bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS customeruserassignment (
    id          bigserial PRIMARY KEY,
    customer_id bigint NOT NULL,
    user_id     bigint NOT NULL,
    role_id     bigint NOT NULL,
    version     bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ldap_group_role_mapping (
    id            bigserial    PRIMARY KEY,
    customer_id   bigint       NOT NULL,
    ldap_group_dn varchar(512) NOT NULL,
    role_id       bigint       NOT NULL,
    created_at    timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_ldap_group_role_mapping UNIQUE (customer_id, ldap_group_dn, role_id)
);

CREATE TABLE IF NOT EXISTS userview (
    id           bigserial PRIMARY KEY,
    customer_id  bigint NOT NULL,
    owner_id     bigint NOT NULL,
    project_id   bigint NOT NULL,
    name         varchar(255) NOT NULL,
    viewtype     bigint NOT NULL DEFAULT 0,
    config       text,
    version      bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS userviewfavorite (
    id      bigserial PRIMARY KEY,
    user_id bigint NOT NULL,
    view_id bigint NOT NULL
);

CREATE TABLE IF NOT EXISTS useritembookmark (
    id          bigserial PRIMARY KEY,
    customer_id bigint NOT NULL,
    user_id     bigint NOT NULL,
    project_id  bigint NOT NULL,
    item_type   varchar(64) NOT NULL,
    item_id     bigint NOT NULL,
    label       varchar(255),
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS welcomepageconfig (
    id          bigserial PRIMARY KEY,
    customer_id bigint NOT NULL,
    user_id     bigint NOT NULL,
    config      text NOT NULL DEFAULT '{}',
    version     bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS userassignedprofile (
    id            bigserial PRIMARY KEY,
    customer_id   bigint NOT NULL,
    user_id       bigint NOT NULL,
    profile_type  varchar(64) NOT NULL,
    assigned_at   timestamptz NOT NULL DEFAULT now()
);

-- Outbox table used by Aqua.Data.Outbox.OutboxMessage. Integration tests that exercise
-- TenantBootstrapper / UserEventPublisher round-trips need this to exist so NHibernate
-- can persist outbox rows alongside the aggregate insert in a single transaction.
CREATE TABLE IF NOT EXISTS messaging_outbox (
    id            uuid          PRIMARY KEY,
    tenant_id     varchar(64)   NOT NULL,
    message_type  varchar(256)  NOT NULL,
    payload       text          NOT NULL,
    headers_json  text          NOT NULL,
    created_at    timestamptz   NOT NULL,
    dispatched_at timestamptz   NULL,
    attempts      int           NOT NULL DEFAULT 0,
    last_error    varchar(2000) NULL
);
CREATE INDEX IF NOT EXISTS ix_outbox_pending ON messaging_outbox (created_at) WHERE dispatched_at IS NULL;
