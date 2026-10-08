---
protocol: along
slug: weak-table-custom-comparer-hashcode-mismatch
type: bug
status: open
priority: high
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [collections, weaktable, equalitycomparer, hashcode, bug, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# WeakTable Breaks Custom IEqualityComparer HashCode Resolution

## Overview
`ActDim.Practix.Collections.Concurrent.WeakTable<K, V>` allows passing a custom `IEqualityComparer<K>`. However, lookup operations compute the hash code using the raw object's default `obj.GetHashCode()` rather than `_comparer.GetHashCode(key)`. This causes a hash bucket mismatch and silent lookup failure whenever a non-default equality comparer (such as case-insensitive string comparer) is provided.

## Identified Defects

### 1. HashCode Computation Bypasses Comparer on Key Lookups
In `WeakTable.cs` (lines 425-435):
```csharp
public int GetHashCode(object obj)
{
    if (obj is WeakReference<State> weakRef)
    {
        var state = Get(weakRef);

        return state == null ? 0 : state.HashCode;
    }

    return obj == null ? 0 : obj.GetHashCode();
}
```
When an item is stored (line 214):
`state.HashCode` is computed via `_comparer.GetHashCode(key)`.
When a key lookup is performed against `_values` (`ConcurrentDictionary<object, WeakReference<State>>`), `obj` is of type `K` (not `WeakReference<State>`).
Line 434 executes:
`return obj == null ? 0 : obj.GetHashCode();`
It calls the default virtual `obj.GetHashCode()`, ignoring `_comparer.GetHashCode((K)obj)`.

### 2. Inconsistent Equals vs GetHashCode Contract
In `EqualityComparer.Equals` (lines 357-418), key comparison correctly invokes `_comparer.Equals(xKey, yKey)`.
Because `Equals` respects `_comparer` while `GetHashCode` does not, two keys that are equal according to `_comparer` (e.g. `"KEY"` and `"key"` under `StringComparer.OrdinalIgnoreCase`) produce different hash codes during lookup, violating the core `GetHashCode` contract of `ConcurrentDictionary`. As a result, lookups fail to find stored entries.

## Remediation Plan
1. Update `EqualityComparer.GetHashCode(object obj)` in `WeakTable.cs`:
```csharp
public int GetHashCode(object obj)
{
    if (obj is WeakReference<State> weakRef)
    {
        var state = Get(weakRef);
        return state == null ? 0 : state.HashCode;
    }

    if (obj is K key)
    {
        return _comparer.GetHashCode(key);
    }

    return obj == null ? 0 : obj.GetHashCode();
}
```
2. Add comprehensive unit tests using a custom case-insensitive string comparer or custom class comparer to verify `TryGetValue`, `ContainsKey`, and `Remove`.

