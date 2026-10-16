---
protocol: along
protocol_version: "4.4.1"
slug: ambient-context-cleanup
type: debt
status: done
completed: 2026-09-30
priority: medium
created: 2026-09-30
updated: 2026-09-30
agent: antigravity
tags: [context, ambient, refactor, api-cleanup, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# AmbientContext Modernization and Static API Cleanup

## Problem Statement / Background
`AmbientContext` retained legacy static convenience methods and redundant properties that mixed execution-flow propagation with direct service discovery. This caused tight coupling across modules, blurred lifetime boundaries, and conflicted with standard dependency injection patterns across server and worker hosting models.

## Implementation Details
1. Cleaned up legacy static accessors on `AmbientContext`, ensuring the class solely manages execution context state and property stack propagation via `AsyncLocal<T>`.
2. Extracted domain-specific context accessors into explicit extension methods under `ActDim.Practix.Abstractions.Context.Extensions`.
3. Standardized lifetime scoping behavior to ensure nested scopes properly restore previous property values upon disposal.
4. Updated unit tests in `AmbientContextTests` to verify scope restoration, thread isolation, and leak-free disposal.

## Acceptance Criteria
- [x] Legacy static service lookup methods removed from `AmbientContext`.
- [x] Context manipulation restricted to clean `Current`, `Properties`, and `PushProperty` semantics.
- [x] Disposing an ambient property scope restores prior ambient state without residual value leaks.
- [x] Unit and integration tests in `AmbientContextTests` pass with zero failures.
