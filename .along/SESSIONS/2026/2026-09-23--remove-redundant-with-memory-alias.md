---
protocol: along
date: 2026-09-23
slug: remove-redundant-with-memory-alias
agent: antigravity
branch: main
commit: pending
summary: Removed redundant WithMemory alias from AmbientContextMemoryExtensions
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [debt--remove-redundant-with-memory-alias]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Remove Redundant WithMemory Alias

## Overview
Removed the redundant `WithMemory` extension method from `ActDim.Practix.Common/Context/Extensions/AmbientContextMemoryExtensions.cs`.
The method was an unnecessary alias for `WithMemoryManager`, conflicted with standard .NET `System.Memory<T>` naming conventions, and was inconsistent with the `With<Resource>Manager` pattern used across all ambient context extensions.

## Accomplishments
1. **API Cleanup**:
   - Removed `public static IDisposable WithMemory(this IAmbientContext context, RecyclableMemoryStreamManager memoryManager)` from `AmbientContextMemoryExtensions.cs`.
2. **Verification**:
   - Confirmed zero remaining references across tests, docs, and codebase.
   - Ran `ActDim.Practix.Common.Tests` with zero failures (249/249 passed).

