---
protocol: along
slug: rename-script-engine-to-emitron
type: debt
status: done
priority: high
created: 2026-08-18
updated: 2026-08-18
completed: 2026-08-18
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# debt--rename-script-engine-to-emitron

## Goal
Rename the primary library facade from `ScriptEngine` to `Emitron` to match the package name (`ActDim.Emitron`) and align with the naming conventions established across the ActDim ecosystem (e.g. `Reflectron` in `ActDim.Reflectron`).

## Summary of Changes
- Renamed `ScriptEngine.cs` to `Emitron.cs` and changed the static class to `Emitron`.
- Added convenience template interpolation facade methods directly on `Emitron` (`Emitron.Interpolate` and `Emitron.CompileTemplate`).
- Updated `Interpolator.cs` and `StringExtensions.cs` to reference `Emitron`.
- Renamed `ScriptEngineTests.cs` to `EmitronTests.cs` and verified all 43 tests passing.
- Updated `README.md` and documentation across projects.
