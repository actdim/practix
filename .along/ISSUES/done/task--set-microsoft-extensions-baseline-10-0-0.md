---
protocol: along
slug: set-microsoft-extensions-baseline-10-0-0
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

# Set Microsoft.Extensions Dependencies Baseline to 10.0.0

## Overview
Align Microsoft.Extensions.* package versions in Directory.Packages.props to 10.0.0 so that consumers of published ActDim NuGet packages can use any 10.x patch version without forced dependency upgrades.

## Tasks
1. [x] Update Microsoft.Extensions.* packages in Directory.Packages.props to 10.0.0.
2. [x] Build the solution via .along/scripts/build.py to verify restore and compilation.
3. [x] Run automated tests via .along/scripts/test.py.
