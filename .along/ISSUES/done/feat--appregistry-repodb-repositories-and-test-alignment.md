---
protocol: along
protocol_version: "2.2.26"
slug: feat--appregistry-repodb-repositories-and-test-alignment
type: feat
status: done
completed: 2026-09-29
priority: high
created: 2026-09-29
updated: 2026-09-29
agent: antigravity
tags: [appregistry, repodb, tests, repositories, iam, postgresql]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Feature: AppRegistry RepoDb Repositories and Test Alignment

## Overview
Update and fix ActDim.AppRegistry.Tests following the recent AppRegistry metamodel refactor and implement RepoDb-backed repositories in ActDim.AppRegistry.Repo:
1. Reference ActDim.Practix.RepoDb in ActDim.AppRegistry.Repo.
2. Implement UserRepo and RoleRepo with RepoDb support (IDbConnection operations for CRUD/queries, with in-memory fallback for parameterless instantiation).
3. Annotate domain models (User, Role, Group) with RepoDb mapping attributes ([Map], [Column], [NotMapped]).
4. Update DomainTests to replace purged Collection test with tests for EntityPermission, AuditLog, EntityTypeDef, and EntityFieldDef.
5. Update RepoTests to test UserRepo and RoleRepo with both in-memory fallback and live RepoDb operations via SQLite in-memory connection.
6. Update ServiceTests to reflect the 2-argument constructor of AppRegistryService(userRepo, roleRepo).
7. Ensure all unit tests in ActDim.AppRegistry.Tests pass.
