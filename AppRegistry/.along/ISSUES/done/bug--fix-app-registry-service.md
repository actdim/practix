---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-app-registry-service
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [app-registry, di, nullability, service]
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Fix AppRegistryService Constructor, Nullable Properties, and DI Registrations

## Problem Description
1. `AppRegistryService` implements `IAppRegistryService` with get-only properties (`Users`, `Roles`, `Projects`), but lacked a constructor accepting dependencies. Consequently, all repository properties remained `null` at runtime.
2. `AppRegistry.Repo` registered repositories in DI as concrete classes (`AddTransient<UserRepo>()`) instead of interface implementations (`IUserRepo`, `IRoleRepo`, `IProjectRepo`), preventing interface-based constructor injection.
3. The old file `AppRegistryService .cs` had a trailing space in its filename.

## Acceptance Criteria
- [x] `AppRegistryService` implements constructor injection for `IUserRepo`, `IRoleRepo`, and `IProjectRepo` with argument validation.
- [x] `AddAppRegistryRepo` registers `IUserRepo`, `IRoleRepo`, `IProjectRepo`, and `CommonRepo`.
- [x] DI resolution of `IAppRegistryService` succeeds with non-null repository instances.
- [x] Solution compiles cleanly with zero errors.

