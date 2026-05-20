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
