---
protocol: along
date: 2026-09-09
slug: fix-sync-over-async-runner
agent: antigravity
branch: main
commit: pending
summary: Fixed sync-over-async in Emitron script execution by precompiling delegates, adding SynchronizationContext protection, and providing async APIs
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [bug--fix-sync-over-async-runner]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Fix sync-over-async in Emitron script execution

## Problem
In `ActDim.Emitron/Emitron.cs`, compiled script delegates called `script.RunAsync(globals).GetAwaiter().GetResult().ReturnValue`.
This introduced:
1. Overhead from creating new `ScriptState` and executing `RunAsync` per invocation rather than reusing `ScriptRunner<T>`.
2. Deadlock risks when executed synchronously on threads with an active `SynchronizationContext`.
3. Lack of asynchronous compilation and evaluation APIs (`CompileAsync<T>`, `EvaluateAsync<T>`).

## Implementation
1. Cached `script.CreateDelegate()` ahead of time during compilation.
2. Protected synchronous execution against `SynchronizationContext` deadlocks via `Task.Run` offload when `SynchronizationContext.Current != null`.
3. Added `CompileAsync<T>` and `EvaluateAsync<T>` overloads.
4. Added comprehensive tests in `Tests/Emitron.Tests/EmitronTests.cs` including single-thread `SynchronizationContext` deadlock prevention.

