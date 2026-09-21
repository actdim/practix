---
protocol: along
slug: ambient-context-push-property-stacking-and-timeout-leak
type: bug
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [context, asynclocal, ambientcontext, leaks, cancellation]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# AmbientContext PushProperty Stacking Bug and Timeout Resource Leak

## Overview
`AmbientContext` manages async-local contextual data (`_current = new AsyncLocal<ImmutableDictionary<string, object>>()`). Code review uncovered a classic non-stacking AsyncLocal bag bug in `PushProperty` where scope disposal can corrupt or prematurely delete outer scope values, along with an unmanaged `CancellationTokenSource` leak in `WithTimeout`.

## Identified Defects

### 1. Non-Stacking `PushProperty` Scope Disposal
In `AmbientContext.cs` (lines 30-51):
```csharp
public IDisposable PushProperty(string name, object value)
{
    Guard.Against.NullOrEmpty(name, nameof(name));

    var previous = _current.Value ?? ImmutableDictionary<string, object>.Empty;
    var existed = previous.TryGetValue(name, out var oldValue);

    _current.Value = previous.SetItem(name, value);

    return new DisposableAction(() =>
    {
        var latest = _current.Value ?? ImmutableDictionary<string, object>.Empty;
        if (existed)
        {
            _current.Value = latest.SetItem(name, oldValue!);
        }
        else
        {
            _current.Value = latest.Remove(name);
        }
    });
}
```
If an outer scope pushes property `X = "A"`, and an inner scope pushes `X = "B"`, disposing the outer scope while the inner scope is still running executes `latest.Remove("X")` because `existed` was `false` in the outer scope. This immediately removes `X` from the inner scope instead of preserving the inner scope's value.
Similarly, nested pushes of the same key without a strict stack structure lead to state corruption when scopes are exited out of order or across asynchronous branches.

### 2. Resource Leak in `WithTimeout` Without Scope Disposal
In `AmbientContext.cs` (lines 118-131):
```csharp
public static IDisposable WithTimeout(TimeSpan timeout, out CancellationToken token)
{
    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
    linkedCts.CancelAfter(timeout);

    token = linkedCts.Token;
    var scope = Current.WithCancellationToken(linkedCts.Token);

    return new DisposableAction(() =>
    {
        scope.Dispose();
        linkedCts.Dispose();
    });
}
```
`WithTimeout` creates a new linked `CancellationTokenSource`. If a caller fails to wrap the returned `IDisposable` in a `using` declaration or block, `linkedCts` leaks until finalization, retaining internal timer queues and event handlers attached to `AmbientContext.CancellationToken`.

## Remediation Plan
1. Refactor ambient property storage to use a stack or immutable list of scopes/frames per key (or an immutable frame stack), ensuring that disposing a scope pops only its corresponding value and leaves active scopes unaffected.
2. Clearly document `WithTimeout` mandatory disposal requirement or provide an overload accepting a delegate action/func (`WithTimeoutAsync(TimeSpan, Func<CancellationToken, Task>)`) to guarantee deterministic disposal of the linked CTS.

