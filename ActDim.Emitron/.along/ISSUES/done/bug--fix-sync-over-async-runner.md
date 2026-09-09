---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-sync-over-async-runner
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [emitron, roslyn, concurrency, deadlock, async]
---

# Fix sync-over-async in Emitron script execution

## Problem
In `Emitron.cs`, compiled script delegates execute `script.RunAsync(globals).GetAwaiter().GetResult().ReturnValue`.
This had three major flaws:
1. It called `script.RunAsync` on every single delegate invocation rather than caching the compiled `ScriptRunner<T>` delegate produced by `script.CreateDelegate()`.
2. Calling `.GetAwaiter().GetResult()` directly on the caller thread under a `SynchronizationContext` risks deadlock if Roslyn continuations marshal back to the context.
3. Emitron lacked first-class async APIs (`CompileAsync` and `EvaluateAsync`) for non-blocking asynchronous evaluation.

## Solution
1. Cache `script.CreateDelegate()` ahead of time during compilation.
2. In synchronous delegate invocation, prevent `SynchronizationContext` deadlocks by checking `SynchronizationContext.Current` and offloading to thread pool via `Task.Run` when a context is present.
3. Add `CompileAsync<T>` and `EvaluateAsync<T>` overloads for non-blocking async execution.
4. Add unit tests in `Tests/Emitron.Tests` verifying async evaluation and deadlock-free invocation under a `SingleThreadSynchronizationContext`.

