---
protocol: along
protocol_version: "2.2.26"
slug: debt--ambient-context-modular-extensions
type: debt
status: done
priority: high
created: 2026-09-22
updated: 2026-09-22
completed: 2026-09-22
agent: antigravity
tags: [ambient-context, architecture, refactoring, abstractions, storage, memory, logging]
related: []
milestone: v2.0.0-along-transition
blocked_by: []
---

# Modularization of AmbientContext and Abstractions Namespace Alignment

## Overview
Refactor `AmbientContext` and its extension methods to eliminate the static Service Locator anti-pattern:
1. Storage contracts in `ActDim.Practix.Abstractions/Storage/` are migrated from `ActDim.BytePath` to `ActDim.Practix.Abstractions.Storage`.
2. `IBufferOwner<T>` is moved from `ActDim.Practix.Common/Memory/` to `ActDim.Practix.Abstractions/Memory/` with namespace `ActDim.Practix.Abstractions.Memory`.
3. Logging extensions are isolated into `ActDim.Practix.Abstractions.Logging`.
4. Core execution flow extensions are isolated into `ActDim.Practix.Abstractions.Context.Extensions`.
5. `AmbientContext` in `ActDim.Practix.Common` is stripped of static service locator properties, retaining only `Current`, `Properties`, and `PushProperty`.

