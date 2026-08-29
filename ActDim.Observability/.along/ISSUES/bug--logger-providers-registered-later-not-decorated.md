---
protocol: along
slug: logger-providers-registered-later-not-decorated
type: bug
status: open
priority: high
created: 2026-08-15
updated: 2026-08-15
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Bug: DI Registration Depends on Call Order and Is Not Idempotent

## Description
Two defects in `AddEventObservability` ([EventObservabilityExtensions.cs](../../ActDim.Observability/EventObservabilityExtensions.cs)):

1. **Order dependency.** `WrapRegisteredLoggerProviders` takes a snapshot of the `ILoggerProvider` descriptors present at the moment of the call. Providers registered afterwards are never decorated, so per-provider suppression silently does nothing for them:

```csharp
services.AddEventObservability();
services.AddLogging(b => b.AddConsole());   // this provider ignores SuppressConsole()
```

The failure mode is silence: no error, the switch simply has no effect.

2. **No idempotence.** Calling `AddEventObservability` twice wraps providers and the factory twice, so tags are written twice and suppression decorators nest.

## Proposal
Decorate at resolution time instead of registration time: for example by registering a decorating `ILoggerFactory`/provider resolver that wraps whatever is in the container when it is built, rather than rewriting descriptors eagerly. Guard the whole registration with a marker service so a second call is a no-op.

## Acceptance
- [ ] A provider registered after `AddEventObservability` honours `SuppressProviders`.
- [ ] Calling `AddEventObservability` twice produces the same behaviour as calling it once.
- [ ] Tests cover both orderings and the double call.
