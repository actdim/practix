# AppRegistry

Reusable multi-project foundation providing universal database schemas, IAM, taxonomy registries, and domain infrastructure on top of PostgreSQL.

## Overview

AppRegistry is an independent, upstream core foundation designed to supply baseline database schemas and domain services for arbitrary backend applications (CRM, ERP, media systems, portals).

AppRegistry contains zero knowledge of downstream consumer applications. Downstream projects consume AppRegistry as a template, shared package, or upstream migration source, extending it with domain-specific schemas.

## Core Architecture

AppRegistry organizes PostgreSQL into dedicated logical schemas:

1. `actdim` (Platform Infrastructure & Subsystem Registry):
   - Subsystem manifest catalog (`actdim.subsystems`) storing registered subsystems (e.g., `app-registry`), semantic versions, schema revisions, and language contract manifests (`JSONB` with C# / TypeScript definitions).
   - Universal migration history ledger (`actdim.migrations`) recording applied migration IDs, subsystem codes, cryptographic checksums (SHA-256), and timestamps.
   - Migration recording stored procedure (`actdim.record_migration(...)`) enforcing integrity checks against script mutation.

2. `iam` (Identity and Access Management):
   - Universal OIDC/vendor-agnostic identity model (`iam.users`, `iam.roles`, `iam.user_roles`, `iam.groups`, `iam.group_members`).
   - Row-Level Security (RLS) policies and session context resolution via `iam.current_user_id()`.
   - Automatic timestamp maintenance triggers (`iam.set_updated_at()`).

3. `registry` (Entity Taxonomy and Polymorphic Registry):
   - Dynamic entity type catalog (`registry.entity_types`) mapping domain types (`code`) to PostgreSQL physical targets (`schema_name`, `table_name`).
   - Entity fields data dictionary (`registry.entity_fields`) storing logical field names, physical columns, display titles, types, nullability, PK flags, and audit flags.
   - Automated schema introspection procedures (`registry.sync_entity_fields(p_entity_code)`, `registry.sync_all_entity_fields()`) populating fields from `pg_catalog`.
   - Hierarchical cataloging with materialized paths (`registry.catalogs`, `registry.catalog_entities`).
   - Curated entity sets (`registry.collections`, `registry.collection_entities`).
   - Polymorphic multi-tenant entity tagging (`registry.entity_tags`).

4. `audit` (Declarative Audit Engine - Planned):
   - Metadata-driven change tracking for sensitive columns configured via `registry.entity_fields.is_audited`.
   - Database triggers recording delta changes, timestamps, and acting `user_id` without blanket table bloat.

## Migrations Workflow (`AppRegistry.Db`)

The canonical baseline database migration lives in `AppRegistry.Db/Migrations/`:

- `20260923230000_app_registry_init_iam_and_registry.up.sql`: Establishes `actdim`, `iam` and `registry` schemas, core tables, indexes, field reflection, and initial seeds.
- `20260923230000_app_registry_init_iam_and_registry.down.sql`: Drops `actdim`, `registry` and `iam` structures cleanly in reverse dependency order.

### Developing and Testing Migrations

When modifying or expanding the foundational schema:

1. Modify the migration files under `AppRegistry.Db/Migrations/`.
2. Follow naming convention: `<timestamp>_<subsystem>_<description>.{up,down}.sql`.
3. Ensure all table names, column names, constraints, and indexes follow snake_case conventions.
4. Keep entity types registered in `registry.entity_types` strictly universal (e.g. `iam:user`, `iam:group`, `registry:catalog`). Never place consumer-specific entity types into AppRegistry migrations.
5. Downstream applications synchronize this baseline migration as their initial migration (step 0), followed by their application-specific migrations.

## Entity Model Conventions

- All registry-managed entities must use `UUID PRIMARY KEY DEFAULT gen_random_uuid()`.
- Tables supporting update tracking must attach trigger `BEFORE UPDATE ... EXECUTE FUNCTION iam.set_updated_at()`.
- Entity type codes follow the namespaced format `<namespace>:<entity>` (e.g., `iam:user`, `registry:collection`).
- Subsystem identifiers follow kebab-case format (e.g., `app-registry`).
