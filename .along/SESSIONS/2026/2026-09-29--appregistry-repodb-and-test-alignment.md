---
protocol: along
slug: appregistry-repodb-and-test-alignment
date: 2026-09-29
agent: antigravity
summary: "AppRegistry RepoDb Repositories and Test Alignment"
---

# Session: AppRegistry RepoDb Repositories and Test Alignment
Date: 2026-09-29
Issue: `feat--appregistry-repodb-repositories-and-test-alignment`

## Objectives
1. Fix broken unit tests in ActDim.AppRegistry.Tests following the AppRegistry metamodel refactoring (removal of Collection, Catalog, and EntityTag).
2. Reference ActDim.Practix.RepoDb in ActDim.AppRegistry.Repo.
3. Annotate domain models (User, Role, Group) with RepoDb attributes ([Table], [Column], [NotMapped]).
4. Implement UserRepo and RoleRepo using RepoDb with IDbConnection operations, retaining in-memory fallback for parameterless construction.
5. Update CommonRepo to query actdim.subsystems via IDbConnection when supplied.
6. Verify and ensure 100% test passes across ActDim.AppRegistry.Tests.

## Changes Made
- ActDim.AppRegistry.Domain:
  - User.cs: mapped to table "users" and defined column mappings for id, external_id, email, display_name, first_name, last_name, avatar_url, is_active, is_email_verified, last_login_at, settings, metadata, created_at, updated_at; marked computed properties (Slug, Name, Username, GivenName, FamilyName, EntityTypeCode) as [NotMapped].
  - Role.cs: mapped to table "roles" and mapped id, name, description, is_system, metadata, created_at, updated_at; marked Slug, IsBuiltin, EntityTypeCode as [NotMapped].
  - Group.cs: mapped to tables "groups" and "group_members", marked EntityTypeCode as [NotMapped].
  - BuiltinRoles.cs: added User, SuperAdmin, Guest constants while preserving Admin.
- ActDim.AppRegistry.Repo:
  - Added project reference to ActDim.Practix.RepoDb.
  - UserRepo.cs: implemented RepoDb CRUD (GetByIdAsync, GetByEmailAsync, GetByExternalIdAsync, GetAllAsync, InsertAsync, UpdateAsync, DeleteByIdAsync) via IDbConnection with in-memory fallback dictionary.
  - RoleRepo.cs: implemented RepoDb CRUD via IDbConnection with in-memory fallback dictionary.
  - CommonRepo.cs: updated GetSchemaVersionAsync to query actdim.subsystems table via IDbConnection when provided.
- Tests/AppRegistry.Tests:
  - ActDim.AppRegistry.Tests.csproj: added package references to Microsoft.Data.Sqlite and RepoDb.Sqlite.Microsoft.
  - DomainTests.cs: removed purged Collection tests; added unit tests for EntityPermission, AuditLog, EntityTypeDef, EntityFieldDef, BuiltinRoles, and EntityType enum members.
  - ServiceTests.cs: updated AppRegistryService instantiation to reflect 2-argument constructor (userRepo, roleRepo) and removed purged Collections references.
  - RepoTests.cs: configured RepoDb SQLite property handlers (SqliteBooleanHandler, SqliteDateTimeOffsetHandler, SqliteNullableDateTimeOffsetHandler, SqliteJsonDocumentHandler); tested ServiceCollection registration, in-memory repository fallbacks, and live RepoDb SQLite CRUD integration for UserRepo and RoleRepo.

## Verification
- Executed `dotnet test D:\Src\my\actdim\public\dotnet\Tests\AppRegistry.Tests\ActDim.AppRegistry.Tests.csproj`:
  - Total: 28 tests, Passed: 28, Failed: 0, Skipped: 0.
- Executed `dotnet build D:\Src\my\actdim\public\dotnet\ActDim.Practix.sln`:
  - 30 projects built successfully with 0 errors.
