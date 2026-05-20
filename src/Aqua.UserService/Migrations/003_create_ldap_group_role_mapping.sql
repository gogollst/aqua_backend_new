CREATE TABLE IF NOT EXISTS ldap_group_role_mapping (
    id            bigserial    PRIMARY KEY,
    customer_id   bigint       NOT NULL REFERENCES customer(id),
    ldap_group_dn varchar(512) NOT NULL,
    role_id       bigint       NOT NULL REFERENCES role(id),
    created_at    timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_ldap_group_role_mapping UNIQUE (customer_id, ldap_group_dn, role_id)
);

CREATE INDEX IF NOT EXISTS idx_ldap_group_role_mapping_customer
    ON ldap_group_role_mapping (customer_id);
