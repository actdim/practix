---
protocol: along
protocol_version: "4.0.0"
slug: task--release-version-1-2-0
type: task
status: done
completed: 2026-09-22
priority: high
created: 2026-09-22
updated: 2026-09-22
agent: antigravity
tags: [release, version-bump, protocol-upgrade]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

## Description
Bump minor version from 1.1.0 to 1.2.0 in Directory.Build.props, upgrade Along protocol to v4.0.0 across all 17 agent contexts, remediate SQLitePCLRaw CVE-2025-6965, synchronize Knowledge Base, record session logs, verify test suites, and commit and push changes.

## Acceptance Criteria
- [x] Bump minor version to 1.2.0 in Directory.Build.props
- [x] Upgrade Along protocol to v4.0.0 across repository and all subprojects
- [x] Remediate SQLitePCLRaw vulnerability (CVE-2025-6965) in Directory.Packages.props
- [x] Create session log .along/SESSIONS/2026/2026-09-22--bump-version-1-2-0.md
- [x] Update .along/HISTORY.md with session entry
- [x] All automated tests pass across ActDim.Practix.sln and ActDim.Three.sln
- [x] Stage, commit, and push to remote repository

