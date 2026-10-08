---
protocol: along
protocol_version: "4.4.7"
slug: consolidate-along-contexts
type: task
status: done
completed: 2026-10-08
priority: high
created: 2026-10-08
updated: 2026-10-08
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

## Description
Consolidate 16 erroneous nested Along installations from package subfolders into root `.along/` created by older versions of Along protocol. Unify all issues, sessions, milestones, decisions, history, and glossary while preserving package-specific guides and documentation.

## Acceptance Criteria
- [x] Migrate all active and done issues to root `.along/ISSUES` with `pkg-*` tags
- [x] Resolve all issue and session collisions without data loss
- [x] Move all session logs to root `.along/SESSIONS`
- [x] Consolidate milestones, decisions (ADR-017..034), and glossary terms
- [x] Preserve package-specific documentation in package `docs/` and package developer guides in `AGENTS.md`
- [x] Purge all 16 nested `.along/` directories, CLAUDE.md files, and .code-review-graph-ignore files
- [x] Verify zero warnings in `along doctor` and `along doctor --entities`
- [x] All solution tests passing via `along test`
