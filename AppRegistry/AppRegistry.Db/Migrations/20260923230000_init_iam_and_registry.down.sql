-- Rollback Migration: 20260923230000_init_iam_and_registry.down.sql
-- Description: Teardown Registry and IAM Schemas

DROP TABLE IF EXISTS registry.entity_tags CASCADE;
DROP TABLE IF EXISTS registry.collection_entities CASCADE;
DROP TABLE IF EXISTS registry.collections CASCADE;
DROP TABLE IF EXISTS registry.catalog_entities CASCADE;
DROP TABLE IF EXISTS registry.catalogs CASCADE;
DROP TABLE IF EXISTS registry.entity_types CASCADE;

DROP TABLE IF EXISTS iam.group_members CASCADE;
DROP TABLE IF EXISTS iam.groups CASCADE;
DROP TABLE IF EXISTS iam.user_roles CASCADE;
DROP TABLE IF EXISTS iam.users CASCADE;
DROP TABLE IF EXISTS iam.roles CASCADE;

DROP FUNCTION IF EXISTS iam.set_updated_at() CASCADE;
DROP FUNCTION IF EXISTS iam.current_user_id() CASCADE;

DROP SCHEMA IF EXISTS registry CASCADE;
DROP SCHEMA IF EXISTS iam CASCADE;
