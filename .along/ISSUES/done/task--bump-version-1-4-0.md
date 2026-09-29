---
protocol: along
protocol_version: "4.4.1"
slug: task--bump-version-1-4-0
type: task
status: done
completed: 2026-09-29
priority: high
created: 2026-09-29
updated: 2026-09-29
agent: antigravity
tags: [release, version-bump, minor]
milestone: v1.4.0-along-protocol-and-iam-alignment
blocked_by: []
related: []
---

# Bump Version to 1.4.0 and Release Reconciliation

## Context
Following Along protocol v4.4.1 upgrade and IAM BuiltinRoles GUID alignment, bump the minor version to 1.4.0, update CHANGELOG.md, and finalize release commit, tag, and push.

## Acceptance Criteria
- [ ] Increment `<Version>` in `Directory.Build.props` to `1.4.0`.
- [ ] Prepend `CHANGELOG.md` with `v1.4.0` section.
- [ ] Reconcile milestone `v1.4.0-along-protocol-and-iam-alignment`.
- [ ] Verify test suite passes with 0 failures.
- [ ] Create release commit, tag `v1.4.0`, and push to remote.
