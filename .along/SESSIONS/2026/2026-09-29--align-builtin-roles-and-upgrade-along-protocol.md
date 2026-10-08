---
protocol: along
slug: align-builtin-roles-and-upgrade-along-protocol
date: 2026-09-29
agent: antigravity
summary: "Align BuiltinRoles GUIDs and Upgrade Along Protocol to v4.4.1"
---

# Session: Align BuiltinRoles GUIDs and Upgrade Along Protocol to v4.4.1
Date: 2026-09-29
Issue: `chore--align-builtin-role-guids-and-upgrade-along-protocol`

## Objectives
1. Align IAM `BuiltinRoles` static GUIDs with database migration seeds (`iam.roles`).
2. Update unit test assertions in `DomainTests` to match deterministic GUIDs.
3. Migrate repository and 16 subproject contexts to Along protocol v4.4.1.
4. Synchronize Knowledge Base and llms-full context.

## Changes
- Updated `AppRegistry.Db/Migrations/20260923230000_app_registry_init_iam_and_registry.sql` with deterministic role GUIDs.
- Updated `AppRegistry.Domain/Iam/BuiltinRoles.cs` matching static GUID fields (`SuperAdmin`, `Admin`, `User`, `Guest`).
- Updated `Tests/AppRegistry.Tests/DomainTests.cs` verifying builtin role identifiers.
- Upgraded `.protocol-version` and protocol blocks across repository root and 16 subprojects to Along v4.4.1.
- Updated Knowledge Base documentation and compiled `llms-full.txt`.

## Verification
- Local tests: `python .along/scripts/test.py` executed across all 12 test projects with 0 failures.
- Typography check: `clean: True findings: 0`.
- Link integrity gate: verified 332 relative Markdown links with 0 broken links.
