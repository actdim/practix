---
protocol: along
slug: reflectron-weakreference-lifecycle-and-unbounded-cache-leak
type: debt
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [reflection, weakreference, memory-leak, caching, assemblyloadcontext]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# IReflectron WeakReference Lifecycle and Unbounded Reflection Caches

## Overview
`Reflectron<T>` wraps target instances with `WeakReference<T>`. When the caller does not retain an independent strong reference, GC collection of the target causes unexpected `ReflectionException("Can't access target object")`. Concurrently, static reflection caches hold permanent strong references in `ConcurrentFactoryDictionary`, causing memory leaks for dynamic/emitted types.

## Identified Defects

### 1. `IReflectron<T>` Retains Only WeakReference to Target
In `Reflectron.Generic.cs` (lines 42-57):
```csharp
private readonly WeakReference<T> _instanceWeakRef;

private T TargetInstance
{
    get
    {
        if (_instanceWeakRef.TryGetTarget(out var instance))
        {
            return instance;
        }

        throw new ReflectionException("Can't access target object");
    }
}
```
An object wrapper is typically expected to hold a strong reference to the wrapped object or offer configurable lifetime tracking. Wrapping a temporary or fluent expression (`new Reflectron<MyClass>(new MyClass())`) results in premature garbage collection of the target instance during GC cycles, throwing `ReflectionException`.

### 2. Unbounded Static Caches Leak Dynamic/Collectible AssemblyLoadContext Types
`Reflectron` maintains static dictionaries (`ConcurrentFactoryDictionary` / `ConcurrentDictionary`):
- `MethodCallerCache`: `ConcurrentFactoryDictionary<MethodInfo, FastMethodCallDelegate>`
- `TypedMethodCallerCache`: `ConcurrentFactoryDictionary<(MethodInfo, Type), Delegate>`
- `TypedPropertyGetterCache`: `ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>`
- `TypedPropertySetterCache`: `ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>`
- `_memberGetterCache`, `_memberSetterCache`, etc.

Because entries are never evicted or held weakly, inspecting methods, properties, or types generated dynamically (e.g. via `Reflection.Emit`, Roslyn scripting, or plugins loaded into collectible `AssemblyLoadContext` instances) permanently anchors their `MethodInfo`, `Type`, and compiled `Delegate` in static memory. This prevents the `AssemblyLoadContext` and its loaded assemblies from ever being unloaded.

## Remediation Plan
1. Re-evaluate `Reflectron<T>` lifetime model: provide strong-reference ownership by default or offer an explicit weak-wrapper variant with clear documentation.
2. For reflection metadata caches, consider using `ConditionalWeakTable` or bounded LRU/clearing mechanisms to avoid anchoring collectible `Type` and `Assembly` instances indefinitely.

