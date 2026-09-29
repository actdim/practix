-- migrate:up
-- Migration: 20260923230000_app_registry_init_iam_and_registry.sql
-- Description: Universal ActDim Platform, IAM and Registry Foundation Schemas with Audit and Permissions
-- Target: PostgreSQL 14+

-- ============================================================================
-- 1. SCHEMAS
-- ============================================================================
CREATE SCHEMA IF NOT EXISTS actdim;
CREATE SCHEMA IF NOT EXISTS iam;
CREATE SCHEMA IF NOT EXISTS registry;

-- ============================================================================
-- 2. HELPER FUNCTIONS
-- ============================================================================

-- Extracts current user UUID from session setting 'app.current_user_id' for RLS
CREATE OR REPLACE FUNCTION iam.current_user_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', true), '')::UUID;
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- Trigger function to automatically maintain updated_at
CREATE OR REPLACE FUNCTION iam.set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 3. ACTDIM PLATFORM INFRASTRUCTURE
-- ============================================================================

-- Registered Subsystems and Language Contract Manifests
CREATE TABLE actdim.subsystems (
    code VARCHAR(100) PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    version VARCHAR(50) NOT NULL,
    schema_version INT NOT NULL DEFAULT 1,
    manifest JSONB NOT NULL DEFAULT '{}'::jsonb,
    installed_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE TRIGGER trg_subsystems_updated_at
BEFORE UPDATE ON actdim.subsystems
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Canonical Subsystem Registration: ActDim AppRegistry
INSERT INTO actdim.subsystems (code, name, version, schema_version, manifest)
VALUES (
    '@actdim/app-registry',
    'ActDim AppRegistry',
    '1.0.0',
    1,
    '{
        "csharp": {
            "rootNamespace": "ActDim.AppRegistry",
            "supportedFeatures": ["iam", "registry", "audit", "permissions"]
        },
        "typescript": {
            "packageName": "@actdim/app-registry",
            "contractsVersion": "1.0.0"
        }
    }'::jsonb
)
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
    version = EXCLUDED.version,
    schema_version = EXCLUDED.schema_version,
    manifest = EXCLUDED.manifest;

-- Universal Migration History Ledger
CREATE TABLE actdim.migrations (
    id VARCHAR(150) PRIMARY KEY,
    subsystem_code VARCHAR(100) NOT NULL REFERENCES actdim.subsystems(code) ON DELETE RESTRICT,
    checksum VARCHAR(128) NOT NULL,
    execution_time_ms INT,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    applied_by VARCHAR(100) NOT NULL DEFAULT CURRENT_USER,
    is_baseline BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX idx_migrations_subsystem ON actdim.migrations (subsystem_code);

-- Migration Registrar Procedure with Integrity Validation
CREATE OR REPLACE FUNCTION actdim.record_migration(
    p_id VARCHAR,
    p_subsystem_code VARCHAR,
    p_checksum VARCHAR,
    p_is_baseline BOOLEAN DEFAULT false,
    p_execution_time_ms INT DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    -- Ensure subsystem entry exists with human-readable name
    INSERT INTO actdim.subsystems (code, name, version)
    VALUES (
        p_subsystem_code,
        INITCAP(REPLACE(REPLACE(REPLACE(p_subsystem_code, '@', ''), '/', ' '), '-', ' ')),
        '1.0.0'
    )
    ON CONFLICT (code) DO NOTHING;

    -- Integrity check: verify checksum for already applied migration
    IF EXISTS (
        SELECT 1 FROM actdim.migrations 
        WHERE id = p_id AND checksum <> p_checksum
    ) THEN
        RAISE EXCEPTION 'Security/Integrity violation: Migration % checksum mismatch! Expected %, but found altered script.', p_id, p_checksum;
    END IF;

    INSERT INTO actdim.migrations (id, subsystem_code, checksum, is_baseline, execution_time_ms)
    VALUES (p_id, p_subsystem_code, p_checksum, p_is_baseline, p_execution_time_ms)
    ON CONFLICT (id) DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- Audit Logging Ledger
CREATE TABLE actdim.audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_code VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(20) NOT NULL, -- 'INSERT', 'UPDATE', 'DELETE'
    actor_user_id UUID,
    old_values JSONB,
    new_values JSONB,
    changed_fields TEXT[],
    actor_ip VARCHAR(45),
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX idx_audit_logs_entity ON actdim.audit_logs (entity_code, entity_id);
CREATE INDEX idx_audit_logs_actor ON actdim.audit_logs (actor_user_id);
CREATE INDEX idx_audit_logs_created ON actdim.audit_logs (created_at DESC);

-- Universal Audit Trigger Function
CREATE OR REPLACE FUNCTION actdim.audit_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_entity_code VARCHAR(100);
    v_entity_id VARCHAR(100);
    v_actor_user_id UUID;
    v_old_values JSONB := NULL;
    v_new_values JSONB := NULL;
    v_changed_fields TEXT[] := ARRAY[]::TEXT[];
    v_col TEXT;
BEGIN
    v_entity_code := TG_ARGV[0];
    v_actor_user_id := iam.current_user_id();

    IF (TG_OP = 'INSERT') THEN
        v_entity_id := NEW.id::VARCHAR;
        v_new_values := to_jsonb(NEW);
        INSERT INTO actdim.audit_logs (entity_code, entity_id, action, actor_user_id, new_values)
        VALUES (v_entity_code, v_entity_id, 'INSERT', v_actor_user_id, v_new_values);
        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        v_entity_id := NEW.id::VARCHAR;
        v_old_values := to_jsonb(OLD);
        v_new_values := to_jsonb(NEW);

        FOR v_col IN SELECT jsonb_object_keys(v_new_values) LOOP
            IF (v_old_values->v_col IS DISTINCT FROM v_new_values->v_col) THEN
                v_changed_fields := array_append(v_changed_fields, v_col);
            END IF;
        END LOOP;

        IF array_length(v_changed_fields, 1) > 0 THEN
            INSERT INTO actdim.audit_logs (entity_code, entity_id, action, actor_user_id, old_values, new_values, changed_fields)
            VALUES (v_entity_code, v_entity_id, 'UPDATE', v_actor_user_id, v_old_values, v_new_values, v_changed_fields);
        END IF;
        RETURN NEW;
    ELSIF (TG_OP = 'DELETE') THEN
        v_entity_id := OLD.id::VARCHAR;
        v_old_values := to_jsonb(OLD);
        INSERT INTO actdim.audit_logs (entity_code, entity_id, action, actor_user_id, old_values)
        VALUES (v_entity_code, v_entity_id, 'DELETE', v_actor_user_id, v_old_values);
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 4. IAM TABLES & PERMISSIONS
-- ============================================================================

-- Security Roles
CREATE TABLE iam.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    is_system BOOLEAN NOT NULL DEFAULT false,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE TRIGGER trg_roles_updated_at
BEFORE UPDATE ON iam.roles
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Seed Default Security Roles
INSERT INTO iam.roles (id, name, description, is_system) VALUES
    ('08a31f2e-dfe6-58f5-bd20-3f90d3e5b473', 'superadmin', 'Unrestricted administrative access to all system tenants and configuration', true),
    ('12440c0b-f84f-5207-b3aa-693d9fa87a9b', 'admin', 'Tenant administrator with management access to identity and content', true),
    ('3aff92c6-f64d-58ea-8330-ef496e6f4616', 'user', 'Standard authenticated user with personal workspace access', true),
    ('398618f5-ff78-54ac-ae27-1a8734465c46', 'guest', 'Read-only access to explicitly published public resources', true)
ON CONFLICT (name) DO UPDATE SET id = EXCLUDED.id, description = EXCLUDED.description, is_system = EXCLUDED.is_system;

-- User Accounts
CREATE TABLE iam.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    zitadel_id VARCHAR(100) UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(100) UNIQUE,
    display_name VARCHAR(150),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url TEXT,
    phone VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    is_phone_verified BOOLEAN NOT NULL DEFAULT false,
    locale VARCHAR(10) NOT NULL DEFAULT 'en',
    timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
    preferred_auth_method VARCHAR(20) NOT NULL DEFAULT 'zitadel',
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX idx_users_zitadel_id ON iam.users (zitadel_id);
CREATE INDEX idx_users_email ON iam.users (email);
CREATE INDEX idx_users_active ON iam.users (is_active) WHERE is_active = true;

CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON iam.users
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Add foreign key constraint from audit_logs to iam.users
ALTER TABLE actdim.audit_logs 
    ADD CONSTRAINT fk_audit_logs_actor 
    FOREIGN KEY (actor_user_id) REFERENCES iam.users(id) ON DELETE SET NULL;

-- User Role Assignments
CREATE TABLE iam.user_roles (
    user_id UUID NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES iam.roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX idx_user_roles_role ON iam.user_roles (role_id);

-- Groups / Communities
CREATE TABLE iam.groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_user_id UUID REFERENCES iam.users(id) ON DELETE SET NULL,
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    avatar_url TEXT,
    is_public BOOLEAN NOT NULL DEFAULT false,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE TRIGGER trg_groups_updated_at
BEFORE UPDATE ON iam.groups
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Group Members
CREATE TABLE iam.group_members (
    group_id UUID NOT NULL REFERENCES iam.groups(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (group_id, user_id)
);

CREATE INDEX idx_group_members_user ON iam.group_members (user_id);

-- Entity Permissions Matrix
CREATE TABLE iam.entity_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL REFERENCES iam.roles(id) ON DELETE CASCADE,
    entity_code VARCHAR(100) NOT NULL,
    can_create BOOLEAN NOT NULL DEFAULT false,
    can_read VARCHAR(10) NOT NULL DEFAULT 'none',   -- 'none' | 'own' | 'group' | 'all'
    can_update VARCHAR(10) NOT NULL DEFAULT 'none', -- 'none' | 'own' | 'group' | 'all'
    can_delete VARCHAR(10) NOT NULL DEFAULT 'none', -- 'none' | 'own' | 'group' | 'all'
    can_export BOOLEAN NOT NULL DEFAULT false,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_role_entity_permission UNIQUE (role_id, entity_code)
);

CREATE INDEX idx_entity_permissions_role ON iam.entity_permissions (role_id);
CREATE INDEX idx_entity_permissions_entity ON iam.entity_permissions (entity_code);

CREATE TRIGGER trg_entity_permissions_updated_at
BEFORE UPDATE ON iam.entity_permissions
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- ============================================================================
-- 5. REGISTRY TABLES & META-MODEL
-- ============================================================================

-- Taxonomy / Entity Type Registry
CREATE TABLE registry.entity_types (
    code VARCHAR(100) PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    schema_name VARCHAR(63) NOT NULL DEFAULT 'public',
    table_name VARCHAR(63),
    pk_column VARCHAR(63) NOT NULL DEFAULT 'id',
    display_field VARCHAR(63) NOT NULL DEFAULT 'name',
    owner_column VARCHAR(63),
    soft_delete_column VARCHAR(63),
    is_system BOOLEAN NOT NULL DEFAULT false,
    is_audited BOOLEAN NOT NULL DEFAULT true,
    icon VARCHAR(50),
    category VARCHAR(50),
    is_nav_visible BOOLEAN NOT NULL DEFAULT true,
    default_sort_field VARCHAR(63) NOT NULL DEFAULT 'created_at',
    default_sort_dir VARCHAR(4) NOT NULL DEFAULT 'desc',
    capabilities JSONB NOT NULL DEFAULT '{"audit": true, "permissions": true}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

-- Seed core entity types (namespaced format domain:entity)
INSERT INTO registry.entity_types (
    code, name, description, schema_name, table_name, pk_column, display_field,
    is_system, is_audited, icon, category, is_nav_visible, default_sort_field, default_sort_dir
) VALUES
    ('iam:user', 'User', 'IAM system user account', 'iam', 'users', 'id', 'display_name', true, true, 'tabler--user', 'Identity', true, 'created_at', 'desc'),
    ('iam:group', 'Group', 'IAM user community or group', 'iam', 'groups', 'id', 'name', true, true, 'tabler--users-group', 'Identity', true, 'created_at', 'desc'),
    ('iam:role', 'Role', 'IAM security role', 'iam', 'roles', 'id', 'name', true, true, 'tabler--shield-lock', 'Identity', true, 'created_at', 'desc'),
    ('iam:entity_permission', 'Entity Permission', 'Granular CRUD access control permission', 'iam', 'entity_permissions', 'id', 'entity_code', true, true, 'tabler--lock-access', 'Security', true, 'created_at', 'desc'),
    ('actdim:audit_log', 'Audit Log', 'System change-tracking audit trail record', 'actdim', 'audit_logs', 'id', 'entity_code', true, false, 'tabler--history', 'System', true, 'created_at', 'desc')
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
    description = EXCLUDED.description,
    schema_name = EXCLUDED.schema_name,
    table_name = EXCLUDED.table_name,
    pk_column = EXCLUDED.pk_column,
    display_field = EXCLUDED.display_field,
    is_system = EXCLUDED.is_system,
    is_audited = EXCLUDED.is_audited,
    icon = EXCLUDED.icon,
    category = EXCLUDED.category,
    is_nav_visible = EXCLUDED.is_nav_visible,
    default_sort_field = EXCLUDED.default_sort_field,
    default_sort_dir = EXCLUDED.default_sort_dir;

-- Entity Fields Registry (Data Dictionary, UI Controls, GraphQL Scalars & Column Mapping)
CREATE TABLE registry.entity_fields (
    entity_type_code VARCHAR(100) NOT NULL REFERENCES registry.entity_types(code) ON DELETE CASCADE,
    name VARCHAR(63) NOT NULL,
    column_name VARCHAR(63),
    display_title VARCHAR(150) NOT NULL,
    data_type VARCHAR(50) NOT NULL,
    scalar_type VARCHAR(30) NOT NULL DEFAULT 'String',
    input_type VARCHAR(50) NOT NULL DEFAULT 'text',
    is_pk BOOLEAN NOT NULL DEFAULT false,
    is_nullable BOOLEAN NOT NULL DEFAULT true,
    is_secret BOOLEAN NOT NULL DEFAULT false,
    is_audited BOOLEAN NOT NULL DEFAULT false,
    is_searchable BOOLEAN NOT NULL DEFAULT false,
    is_filterable BOOLEAN NOT NULL DEFAULT false,
    is_readonly BOOLEAN NOT NULL DEFAULT false,
    show_in_list BOOLEAN NOT NULL DEFAULT true,
    show_in_form BOOLEAN NOT NULL DEFAULT true,
    options JSONB NOT NULL DEFAULT '[]'::jsonb,
    help_text TEXT,
    placeholder VARCHAR(150),
    fk_target_entity_code VARCHAR(100) REFERENCES registry.entity_types(code) ON DELETE SET NULL,
    sort_order INT NOT NULL DEFAULT 0,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (entity_type_code, name)
);

CREATE INDEX idx_entity_fields_audited ON registry.entity_fields (entity_type_code) WHERE is_audited = true;
CREATE INDEX idx_entity_fields_fk ON registry.entity_fields (fk_target_entity_code) WHERE fk_target_entity_code IS NOT NULL;

CREATE TRIGGER trg_entity_fields_updated_at
BEFORE UPDATE ON registry.entity_fields
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Schema Introspection Function: Synchronize Entity Fields from Database Catalog
CREATE OR REPLACE FUNCTION registry.sync_entity_fields(p_entity_code VARCHAR)
RETURNS INT AS $$
DECLARE
    v_schema VARCHAR(63);
    v_table VARCHAR(63);
    v_pk_column VARCHAR(63);
    v_relid OID;
    v_count INT := 0;
BEGIN
    SELECT schema_name, table_name, pk_column
    INTO v_schema, v_table, v_pk_column
    FROM registry.entity_types
    WHERE code = p_entity_code;

    IF v_schema IS NULL OR v_table IS NULL THEN
        RETURN 0;
    END IF;

    SELECT c.oid INTO v_relid
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = v_schema AND c.relname = v_table;

    IF v_relid IS NULL THEN
        RETURN 0;
    END IF;

    -- Upsert columns from pg_attribute into registry.entity_fields
    INSERT INTO registry.entity_fields (
        entity_type_code,
        name,
        column_name,
        display_title,
        data_type,
        scalar_type,
        input_type,
        is_pk,
        is_nullable,
        is_secret,
        is_searchable,
        is_filterable,
        show_in_list,
        show_in_form,
        placeholder,
        fk_target_entity_code,
        sort_order
    )
    SELECT
        p_entity_code,
        a.attname,
        a.attname,
        INITCAP(REPLACE(a.attname, '_', ' ')),
        format_type(a.atttypid, a.atttypmod),
        -- GraphQL / JSON Schema scalar mapping
        CASE
            WHEN pk.is_pk AND a.atttypid = 'uuid'::regtype THEN 'ID'
            WHEN a.atttypid = 'uuid'::regtype THEN 'ID'
            WHEN a.atttypid IN ('int2'::regtype, 'int4'::regtype) THEN 'Int'
            WHEN a.atttypid IN ('int8'::regtype, 'numeric'::regtype, 'float4'::regtype, 'float8'::regtype) THEN 'Float'
            WHEN a.atttypid = 'bool'::regtype THEN 'Boolean'
            WHEN a.atttypid IN ('timestamp'::regtype, 'timestamptz'::regtype) THEN 'DateTime'
            WHEN a.atttypid = 'date'::regtype THEN 'Date'
            WHEN a.atttypid IN ('json'::regtype, 'jsonb'::regtype) THEN 'JSON'
            ELSE 'String'
        END,
        -- UI input widget mapping
        CASE
            WHEN fk.target_code IS NOT NULL THEN 'select'
            WHEN a.atttypid = 'bool'::regtype THEN 'checkbox'
            WHEN a.atttypid IN ('int2'::regtype, 'int4'::regtype, 'int8'::regtype, 'numeric'::regtype, 'float4'::regtype, 'float8'::regtype) THEN 'number'
            WHEN a.atttypid IN ('timestamp'::regtype, 'timestamptz'::regtype) THEN 'datetime'
            WHEN a.atttypid = 'date'::regtype THEN 'date'
            WHEN a.atttypid IN ('json'::regtype, 'jsonb'::regtype) THEN 'json'
            WHEN a.atttypid = 'text'::regtype AND a.attname NOT IN ('id', 'external_id') THEN 'textarea'
            ELSE 'text'
        END,
        COALESCE(pk.is_pk, false),
        NOT a.attnotnull,
        -- Auto-detect secret fields by naming convention
        CASE
            WHEN a.attname ILIKE '%password%' OR a.attname ILIKE '%secret%' OR a.attname ILIKE '%credential%' OR a.attname ILIKE '%private_key%' THEN true
            ELSE false
        END,
        -- Auto-detect global text searchable fields
        CASE
            WHEN a.attname IN ('name', 'display_name', 'email', 'slug', 'title') THEN true
            ELSE false
        END,
        -- Auto-detect filterable fields (keys, enums, flags, status)
        CASE
            WHEN pk.is_pk THEN true
            WHEN fk.target_code IS NOT NULL THEN true
            WHEN a.atttypid = 'bool'::regtype THEN true
            WHEN a.attname IN ('status', 'node_type', 'drive_type', 'provider_type', 'auth_provider', 'is_active', 'is_default') THEN true
            ELSE false
        END,
        -- show_in_list: exclude secrets and heavy fields
        CASE
            WHEN a.attname ILIKE '%password%' OR a.attname ILIKE '%secret%' OR a.attname ILIKE '%credential%' OR a.attname ILIKE '%private_key%' THEN false
            WHEN a.attname IN ('metadata', 'description', 'avatar_url') THEN false
            ELSE true
        END,
        -- show_in_form: exclude generated primary keys and timestamps
        CASE
            WHEN pk.is_pk THEN false
            WHEN a.attname IN ('created_at', 'updated_at', 'deleted_at') THEN false
            ELSE true
        END,
        'Enter ' || INITCAP(REPLACE(a.attname, '_', ' ')),
        fk.target_code,
        a.attnum
    FROM pg_attribute a
    LEFT JOIN (
        SELECT 
            conrelid,
            unnest(conkey) AS col_attnum,
            true AS is_pk
        FROM pg_constraint
        WHERE conrelid = v_relid AND contype = 'p'
    ) pk ON pk.conrelid = a.attrelid AND pk.col_attnum = a.attnum
    LEFT JOIN (
        SELECT 
            c.conrelid,
            c.conkey[1] AS col_attnum,
            target_et.code AS target_code
        FROM pg_constraint c
        JOIN pg_class ref_c ON ref_c.oid = c.confrelid
        JOIN pg_namespace ref_n ON ref_n.oid = ref_c.relnamespace
        JOIN registry.entity_types target_et 
            ON target_et.schema_name = ref_n.nspname 
           AND target_et.table_name = ref_c.relname
        WHERE c.contype = 'f' AND array_length(c.conkey, 1) = 1
    ) fk ON fk.conrelid = a.attrelid AND fk.col_attnum = a.attnum
    WHERE a.attrelid = v_relid
      AND a.attnum > 0
      AND NOT a.attisdropped
    ON CONFLICT (entity_type_code, name) DO UPDATE
    SET column_name = EXCLUDED.column_name,
        data_type = EXCLUDED.data_type,
        scalar_type = EXCLUDED.scalar_type,
        is_pk = EXCLUDED.is_pk,
        is_nullable = EXCLUDED.is_nullable,
        show_in_list = EXCLUDED.show_in_list,
        show_in_form = EXCLUDED.show_in_form,
        placeholder = EXCLUDED.placeholder,
        fk_target_entity_code = COALESCE(registry.entity_fields.fk_target_entity_code, EXCLUDED.fk_target_entity_code),
        sort_order = EXCLUDED.sort_order,
        updated_at = clock_timestamp();

    GET DIAGNOSTICS v_count = ROW_COUNT;

    -- Prune physical columns that were removed from the physical table
    DELETE FROM registry.entity_fields ef
    WHERE ef.entity_type_code = p_entity_code
      AND ef.column_name IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM pg_attribute a
          WHERE a.attrelid = v_relid
            AND a.attname = ef.column_name
            AND a.attnum > 0
            AND NOT a.attisdropped
      );

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- Batch Synchronization Function for All Registered Entity Types
CREATE OR REPLACE FUNCTION registry.sync_all_entity_fields()
RETURNS INT AS $$
DECLARE
    r RECORD;
    v_total INT := 0;
    v_cnt INT := 0;
BEGIN
    FOR r IN SELECT code FROM registry.entity_types WHERE table_name IS NOT NULL LOOP
        v_cnt := registry.sync_entity_fields(r.code);
        v_total := v_total + v_cnt;
    END LOOP;
    RETURN v_total;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 6. INITIAL POST-DEPLOYMENT HOOKS
-- ============================================================================

-- Automatically introspect and populate fields for all initial entity types
SELECT registry.sync_all_entity_fields();

-- Record baseline migration in the unified ledger
SELECT actdim.record_migration(
    '20260923230000_app_registry_init_iam_and_registry',
    '@actdim/app-registry',
    'sha256:20260923230000_app_registry_init_iam_and_registry_baseline',
    true
);

-- migrate:down
-- Rollback Migration: 20260923230000_app_registry_init_iam_and_registry.sql
-- Description: Teardown ActDim Platform, Registry, and IAM Schemas

DROP TABLE IF EXISTS registry.entity_fields CASCADE;
DROP TABLE IF EXISTS registry.entity_types CASCADE;

DROP FUNCTION IF EXISTS registry.sync_all_entity_fields() CASCADE;
DROP FUNCTION IF EXISTS registry.sync_entity_fields(VARCHAR) CASCADE;

DROP TABLE IF EXISTS iam.entity_permissions CASCADE;
DROP TABLE IF EXISTS iam.group_members CASCADE;
DROP TABLE IF EXISTS iam.groups CASCADE;
DROP TABLE IF EXISTS iam.user_roles CASCADE;
DROP TABLE IF EXISTS iam.users CASCADE;
DROP TABLE IF EXISTS iam.roles CASCADE;

DROP FUNCTION IF EXISTS iam.set_updated_at() CASCADE;
DROP FUNCTION IF EXISTS iam.current_user_id() CASCADE;

DROP FUNCTION IF EXISTS actdim.audit_trigger() CASCADE;
DROP TABLE IF EXISTS actdim.audit_logs CASCADE;

DROP FUNCTION IF EXISTS actdim.record_migration(VARCHAR, VARCHAR, VARCHAR, BOOLEAN, INT) CASCADE;
DROP TABLE IF EXISTS actdim.migrations CASCADE;
DROP TABLE IF EXISTS actdim.subsystems CASCADE;

DROP SCHEMA IF EXISTS actdim CASCADE;
DROP SCHEMA IF EXISTS registry CASCADE;
DROP SCHEMA IF EXISTS iam CASCADE;
