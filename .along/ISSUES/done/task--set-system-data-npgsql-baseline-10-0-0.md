---
protocol: along
slug: set-system-data-npgsql-baseline-10-0-0
type: task
status: done
priority: medium
created: 2026-09-15
updated: 2026-09-15
completed: 2026-09-15
agent: antigravity
tags: [cpm, nuget, dependencies]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Set System, Microsoft.Data.Sqlite and Npgsql Baseline to 10.0.0

## Overview
Update System.IO.Hashing, System.Runtime.Caching, Microsoft.Data.Sqlite, and Npgsql in Directory.Packages.props to lowest compatible baselines.

## Tasks
1. [x] Update System.IO.Hashing, System.Runtime.Caching, Npgsql to 10.0.0.
2. [x] Set Microsoft.Data.Sqlite to 10.0.9 (lowest version satisfying transitive dependency RepoDb.Sqlite.Microsoft 1.15.0).
3. [x] Verify build and restore via .along/scripts/build.py.
4. [x] Run tests via .along/scripts/test.py.

