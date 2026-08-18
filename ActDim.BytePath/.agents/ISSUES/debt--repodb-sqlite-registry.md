---
slug: repodb-sqlite-registry
type: debt
status: open
priority: medium
created: 2026-08-18
updated: 2026-08-18
---

# debt--repodb-sqlite-registry

## Goal
Refactor `ActDim.BytePath.SqliteRegistry` to use **RepoDb** (with `Microsoft.Data.Sqlite` / `RepoDb.SQLite.Microsoft`) instead of `sqlite-net-pcl` for SQLite database interactions, boosting performance, connection management resilience, and query execution efficiency.

## Context & Motivation
- `SQLiteBlobRegistry` currently relies on `sqlite-net-pcl` (`SQLiteAsyncConnection`), which has constrained connection pooling and ORM customization.
- **RepoDb** is a lightweight, ultra-fast micro-ORM for .NET with compiled IL execution, zero-overhead mapping, fluent table/column configuration, raw SQL escape hatches, and first-class async connection lifecycle management.
- Migrating to RepoDb standardizes database access on modern ADO.NET (`Microsoft.Data.Sqlite`), improves concurrent throughput under high lock contention, and simplifies future extensions for PostgreSQL/SQL Server registries.

## Key Requirements & Invariants
1. **Preserve Database Schema & Compatibility:**
   - Table: `blob_records`
   - Columns: `blob_key` (PK), `metadata`, `content_type`, `size`, `hash`, `created_at`, `updated_at`, `accessed_at`, `sliding_expiration_seconds`, `expires_at`, and lock columns (`lock_type`, `lock_holder`, `locked_at`, etc.).
2. **Locking & Concurrency Guarantees:**
   - Shared read locks and exclusive write locks must maintain timeout handling and atomic state transitions without deadlocks.
3. **TTL & Expiration:**
   - Background cleanup and sliding expiration renewal logic must execute efficiently via parameterized SQL / RepoDb operations.
4. **Zero Regression:**
   - All 74 existing unit tests in `ActDim.BytePath.Tests` must continue to pass seamlessly.

## Implementation Steps
1. **Package Updates:**
   - Remove `sqlite-net-pcl` from `ActDim.BytePath.SqliteRegistry.csproj`.
   - Add `RepoDb` and `RepoDb.SQLite.Microsoft` / `Microsoft.Data.Sqlite`.
2. **Bootstrapping & Mapping:**
   - Ensure `RepoDb.SQLiteBootstrap.Initialize()` is called on startup.
   - Configure model mappings for `BlobRecordTransport` / `BlobRecord` via RepoDb fluent/attribute configuration.
3. **Refactor `SQLiteBlobRegistry`:**
   - Replace `SQLiteAsyncConnection` with `SqliteConnection` factories.
   - Implement table creation (`CREATE TABLE IF NOT EXISTS blob_records ...` and index definitions).
   - Rewrite CRUD queries (`GetAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`).
   - Rewrite lock acquisition, heartbeat renewal, and expiration sweeps using RepoDb methods or optimized raw SQL.
4. **Testing & Verification:**
   - Run `dotnet test Tests/BytePath.Tests/ActDim.BytePath.Tests.csproj` and full solution test suites.
   - Validate performance under concurrent load.
   - Update `ActDim.BytePath.SqliteRegistry/README.md`.

## Files Touched
- `ActDim.BytePath.SqliteRegistry/ActDim.BytePath.SqliteRegistry.csproj`
- `ActDim.BytePath.SqliteRegistry/SQLiteBlobRegistry.cs`
- `ActDim.BytePath.SqliteRegistry/README.md`
- `Tests/BytePath.Tests/*` (verification)
