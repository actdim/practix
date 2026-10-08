---
protocol: along
protocol_version: "2.2.26"
slug: task--move-ambient-context-memory-extensions
type: task
status: done
completed: 2026-09-21
priority: low
created: 2026-09-21
updated: 2026-09-21
agent: antigravity
tags: [context, extensions, refactor, file-structure, pkg-practix-common]
related: []
milestone: v2.0.0-along-transition
blocked_by: []
---

# Move AmbientContextMemoryExtensions to Context/Extensions

## Overview
Move `AmbientContextMemoryExtensions.cs` to `Context/Extensions/` subfolder in `ActDim.Practix.Common` to align with the subsystem extension convention (matching `Introspection/Extensions/`) while preserving the `ActDim.Practix.Context` namespace.

## Requirements
1. Move `Context/AmbientContextMemoryExtensions.cs` to `Context/Extensions/AmbientContextMemoryExtensions.cs`.
2. Keep namespace as `ActDim.Practix.Context` to avoid breaking changes or requiring extra using directives for callers.
3. Verify builds and tests pass cleanly.
