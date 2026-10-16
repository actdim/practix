---
protocol: along
protocol_version: "4.4.7"
slug: notify-disposed-interface
type: task
status: done
completed: 2026-10-08
priority: medium
created: 2026-10-08
updated: 2026-10-08
agent: antigravity
tags: [disposal, abstractions]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Replace IObservableDisposable with INotifyDisposed

## Overview
Replace `IObservableDisposable` with `INotifyDisposed` in `ActDim.Practix.Abstractions/Disposal/`.
Decouple disposal notifications from synchronous `IDisposable` by having `INotifyDisposed` inherit solely from `IDisposedState`. This enables use with both `IDisposable` and `IAsyncDisposable` implementations and adheres to ISP.

## Acceptance Criteria
- [x] `INotifyDisposed.cs` created under `ActDim.Practix.Abstractions.Disposal` namespace extending `IDisposedState`.
- [x] `IObservableDisposable.cs` deleted.
- [x] `DisposalTests.cs` updated to test `INotifyDisposed`.
- [x] Documentation updated in `ActDim.Practix.Abstractions/README.md`.
- [x] Solution compiles and all tests pass.
