---
protocol: along
protocol_version: "2.2.26"
slug: debt--remove-redundant-with-memory-alias
type: debt
status: done
priority: low
created: 2026-09-23
updated: 2026-09-23
completed: 2026-09-23
agent: antigravity
tags: [ambient-context, memory, api-cleanup, technical-debt]
related: [debt--move-recyclable-memory-stream-extensions-to-common]
milestone: v2.0.0-along-transition
blocked_by: []
---

# Remove Redundant WithMemory Alias from AmbientContextMemoryExtensions

## Overview
Remove the redundant `WithMemory` alias extension method from `ActDim.Practix.Common/Context/Extensions/AmbientContextMemoryExtensions.cs`.
The method was an unnecessary alias for `WithMemoryManager`, caused semantic ambiguity with standard .NET `System.Memory<T>`, and broke naming consistency across `With<Resource>Manager` ambient extensions.

## Requirements
1. Remove `public static IDisposable WithMemory(this IAmbientContext context, RecyclableMemoryStreamManager memoryManager)` from `AmbientContextMemoryExtensions.cs`.
2. Verify no references remain in tests, documentation, or codebase.
3. Verify test suite passes without regressions.

