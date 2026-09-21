---
protocol: along
slug: concurrent-factory-dictionary-contract-and-lazy-pitfalls
type: bug
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [collections, concurrentfactorydictionary, lazy, disposal, dictionary]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# ConcurrentFactoryDictionary Indexer, Disposal, and Faulted Lazy Handling

## Overview
`ActDim.Practix.Collections.Concurrent.ConcurrentFactoryDictionary<TKey, TValue>` is designed as a thread-safe factory-backed dictionary wrapping `Lazy<TValue>`. Analysis identified three contract and runtime flaws: the indexer does not trigger the factory, removal/clearing does not dispose disposable values, and `TryGetValue` forces `Lazy.Value` with unhandled exception and lazy corruption risks.

## Identified Defects

### 1. Indexer Does Not Invoke Factory
In `ConcurrentFactoryDictionary.cs` (line 50):
```csharp
public TValue this[TKey key] => _dictionary[key].Value;
```
Accessing `dict[key]` for a missing key throws `KeyNotFoundException` instead of calling `GetOrCreateValue(key)`. For a collection explicitly named `ConcurrentFactoryDictionary`, users expect the indexer to either auto-create the entry via factory or have consistent factory semantics.

### 2. `Clear` and `Remove` Do Not Dispose Values
In `ConcurrentFactoryDictionary.cs` (lines 79-82, 129-132):
`Clear()` and `Remove(key)` clear or remove entries from `_dictionary` without checking if `TValue` implements `IDisposable` or `IAsyncDisposable`. If values own unmanaged handles, sockets, or memory buffers, removing them from the dictionary causes resource leaks.

### 3. `TryGetValue` Forces `Lazy.Value` Without Fault Recovery
In `ConcurrentFactoryDictionary.cs` (lines 158-168):
```csharp
public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
{
    if (_dictionary.TryGetValue(key, out var container))
    {
        value = container.Value;
        return true;
    }

    value = default;
    return false;
}
```
If a `container` was added via a raw method or if `container.Value` throws an exception during factory evaluation, `TryGetValue` throws an unhandled exception rather than returning false or recovering. Unlike `GetOrCreateValue` (lines 117-122), it does not remove the faulted `Lazy<TValue>` from `_dictionary`, permanently poisoning that key with a cached exception.

## Remediation Plan
1. Clarify and align the indexer contract (call `GetOrCreateValue(key)` or document why direct dictionary indexing throws).
2. If `typeof(IDisposable).IsAssignableFrom(typeof(TValue))`, dispose removed/cleared values when their `Lazy.IsValueCreated` is true.
3. Protect `TryGetValue` against faulted `container.Value` and ensure failed lazy entries are evicted.

