---
protocol: along
protocol_version: "2.2.26"
slug: task--bump-version-1-3-0
type: task
status: done
priority: high
created: 2026-09-24
updated: 2026-09-24
completed: 2026-09-24
agent: antigravity
tags: [release, version-bump, namespace-refactoring]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Bump Version to 1.3.0 and Release Reconciliation

## Context
Following comprehensive namespace unification (`ActDim.Practix`), specialized collection refactoring (`StaticMap`), type exposure (`BlobManager`, `BlobManagerBuilder`, `CoreJsonSerializer`), and ASCII typography cleanup, bump the minor version to `1.3.0` and finalize release artifacts.

## Acceptance Criteria
- [x] Increment `<Version>` in `Directory.Build.props` to `1.3.0`.
- [x] Update `CHANGELOG.md` with `v1.3.0` section.
- [x] Record session log `.along/SESSIONS/2026/2026-09-24--bump-version-1-3-0.md`.
- [x] Verify test suite passes (0 failures).
- [x] Stage all modified and new files, commit, and push to remote.
