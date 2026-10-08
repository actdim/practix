---
protocol: along
protocol_version: "4.4.1"
slug: chore--align-builtin-role-guids-and-upgrade-along-protocol
type: task
status: done
completed: 2026-09-29
priority: medium
created: 2026-09-29
updated: 2026-09-29
agent: antigravity
tags: [iam, appregistry, protocol, along]
blocked_by: []
milestone: v1.3.0-knowledge-base-and-graph
related: []
---

# Align BuiltinRoles GUIDs and Upgrade Along Protocol to v4.4.1

## Context
Reconcile IAM security roles with persistent migration seeds and upgrade repository agent contexts and Knowledge Base to Along protocol v4.4.1.

## Acceptance Criteria
- [x] Update `iam.roles` seed data and `BuiltinRoles` static GUID identifiers to deterministic values.
- [x] Align `DomainTests` assertions with deterministic builtin role GUIDs.
- [x] Upgrade repository root and subprojects to Along protocol v4.4.1.
- [x] Synchronize Knowledge Base link graph and full text context.
