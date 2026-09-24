-- Migration: 20260923230000_init_iam_and_registry.up.sql
-- Description: Universal IAM and Registry Foundation Schemas
-- Target: PostgreSQL 14+

-- ============================================================================
-- 1. SCHEMAS
-- ============================================================================
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
-- 3. IAM TABLES
-- ============================================================================

-- Security Roles
CREATE TABLE iam.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    is_builtin BOOLEAN NOT NULL DEFAULT false,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE TRIGGER trg_roles_updated_at
BEFORE UPDATE ON iam.roles
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Seed canonical BuiltinRoles.Admin (matches ActDim.AppRegistry.Domain.Iam.BuiltinRoles)
INSERT INTO iam.roles (id, name, slug, description, is_builtin)
VALUES ('d413f443-1c49-4d84-82fc-054b9e063c21', 'Administrator', 'admin', 'Super administrator with full access', true);

-- Users (Canonical OIDC / Vendor-agnostic)
CREATE TABLE iam.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id TEXT NOT NULL UNIQUE,
    auth_provider VARCHAR(50) NOT NULL DEFAULT 'zitadel',
    org_id TEXT,
    email TEXT UNIQUE,
    display_name VARCHAR(255) NOT NULL,
    first_name VARCHAR(150),
    last_name VARCHAR(150),
    avatar_url TEXT,
    roles TEXT[] NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    last_login_at TIMESTAMPTZ,
    settings JSONB NOT NULL DEFAULT '{}'::jsonb,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX idx_users_email_lower ON iam.users (LOWER(email));
CREATE INDEX idx_users_external_id ON iam.users (external_id);
CREATE INDEX idx_users_org_id ON iam.users (org_id) WHERE org_id IS NOT NULL;
CREATE INDEX idx_users_settings_gin ON iam.users USING gin (settings);
CREATE INDEX idx_users_metadata_gin ON iam.users USING gin (metadata);

CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON iam.users
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Enable Row Level Security (RLS) on users
ALTER TABLE iam.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY users_select_policy ON iam.users
    FOR SELECT
    USING (
        id = iam.current_user_id()
        OR iam.current_user_id() IS NULL
    );

CREATE POLICY users_update_policy ON iam.users
    FOR UPDATE
    USING (id = iam.current_user_id())
    WITH CHECK (id = iam.current_user_id());

-- User Role Assignments
CREATE TABLE iam.user_roles (
    user_id UUID NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES iam.roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (user_id, role_id)
);

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

-- Group Members (membership relation)
CREATE TABLE iam.group_members (
    group_id UUID NOT NULL REFERENCES iam.groups(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (group_id, user_id)
);

CREATE INDEX idx_group_members_user ON iam.group_members (user_id);

-- ============================================================================
-- 4. REGISTRY TABLES
-- ============================================================================

-- Taxonomy / Entity Type Registry
CREATE TABLE registry.entity_types (
    code VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

-- Seed core entity types (namespaced format domain:entity)
INSERT INTO registry.entity_types (code, name, description) VALUES
    ('iam:user', 'User', 'IAM system user account'),
    ('iam:group', 'Group', 'IAM user community or group'),
    ('iam:role', 'Role', 'IAM security role'),
    ('registry:catalog', 'Catalog', 'Registry hierarchical category/catalog node'),
    ('registry:collection', 'Collection', 'Registry curated entity collection');

-- Catalogs (Hierarchical taxonomy with UUID PK + Materialized Path)
CREATE TABLE registry.catalogs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id UUID REFERENCES registry.catalogs(id) ON DELETE CASCADE,
    slug VARCHAR(100) NOT NULL,
    path TEXT NOT NULL,
    name VARCHAR(255) NOT NULL,
    owner_user_id UUID REFERENCES iam.users(id) ON DELETE CASCADE,
    description TEXT,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_catalogs_parent_slug UNIQUE (parent_id, slug)
);

CREATE INDEX idx_catalogs_path ON registry.catalogs (path text_pattern_ops);
CREATE INDEX idx_catalogs_parent_id ON registry.catalogs (parent_id);

CREATE TRIGGER trg_catalogs_updated_at
BEFORE UPDATE ON registry.catalogs
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Entities linked to Catalogs
CREATE TABLE registry.catalog_entities (
    catalog_id UUID NOT NULL REFERENCES registry.catalogs(id) ON DELETE CASCADE,
    entity_type_code VARCHAR(50) NOT NULL REFERENCES registry.entity_types(code) ON DELETE RESTRICT,
    entity_id UUID NOT NULL,
    added_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (catalog_id, entity_type_code, entity_id)
);

CREATE INDEX idx_catalog_entities_lookup ON registry.catalog_entities (entity_type_code, entity_id);

-- Collections (Named curated sets of entities)
CREATE TABLE registry.collections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_user_id UUID REFERENCES iam.users(id) ON DELETE CASCADE,
    group_id UUID REFERENCES iam.groups(id) ON DELETE CASCADE,
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(150) NOT NULL,
    description TEXT,
    allowed_entity_type VARCHAR(50) REFERENCES registry.entity_types(code) ON DELETE RESTRICT,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX idx_collections_owner ON registry.collections (owner_user_id);
CREATE INDEX idx_collections_group ON registry.collections (group_id) WHERE group_id IS NOT NULL;

CREATE TRIGGER trg_collections_updated_at
BEFORE UPDATE ON registry.collections
FOR EACH ROW EXECUTE FUNCTION iam.set_updated_at();

-- Collection Entities
CREATE TABLE registry.collection_entities (
    collection_id UUID NOT NULL REFERENCES registry.collections(id) ON DELETE CASCADE,
    entity_type_code VARCHAR(50) NOT NULL REFERENCES registry.entity_types(code) ON DELETE RESTRICT,
    entity_id UUID NOT NULL,
    sort_order INT NOT NULL DEFAULT 0,
    added_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (collection_id, entity_type_code, entity_id)
);

CREATE INDEX idx_collection_entities_order ON registry.collection_entities (collection_id, sort_order);

-- Polymorphic Entity Tags
CREATE TABLE registry.entity_tags (
    entity_type_code VARCHAR(50) NOT NULL REFERENCES registry.entity_types(code) ON DELETE RESTRICT,
    entity_id UUID NOT NULL,
    tag VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (entity_type_code, entity_id, tag)
);

CREATE INDEX idx_entity_tags_tag ON registry.entity_tags (tag);
