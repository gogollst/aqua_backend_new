ALTER TABLE aquauser
    ADD COLUMN IF NOT EXISTS ldap_dn varchar(512) NULL;

CREATE INDEX IF NOT EXISTS idx_aquauser_ldap_dn
    ON aquauser (ldap_dn) WHERE ldap_dn IS NOT NULL;
