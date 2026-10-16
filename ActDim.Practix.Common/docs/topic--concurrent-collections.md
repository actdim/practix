---
protocol: along
slug: concurrent-collections
title: Specialized Concurrent Collections
type: topic
created: 2026-09-03
updated: 2026-09-09
tags: [collections, concurrent, weak-table, composite-key, lock-free]
---

# Specialized Concurrent Collections

`ActDim.Practix.Collections` provides optimized concurrent collections, weak tables with non-identity equality, and composite keys designed for thread safety, low allocation, and cache efficiency.

---

## Concurrent Factory Dictionary (`ConcurrentFactoryDictionary<TKey, TValue>`)

A high-performance thread-safe dictionary backed by `ConcurrentDictionary<TKey, Lazy<TValue>>` that guarantees **exactly-once** value initialization:

```csharp
var cache = new ConcurrentFactoryDictionary<string, UserSession>(
    key => LoadSessionFromDatabase(key),
    StringComparer.OrdinalIgnoreCase
);

// Returns existing session or computes it once thread-safely
UserSession session = cache.GetOrCreateValue("session_981");
```

### Key Guarantees:
- **No Double Computation**: Multiple threads requesting the same missing key simultaneously do not invoke the factory multiple times (uses `LazyThreadSafetyMode.ExecutionAndPublication`).
- **Exception Resilience**: If the factory throws an exception during value creation, the failed `Lazy` instance is immediately removed from the dictionary so subsequent calls can retry instead of caching a fault.
- **Read-Only Interface**: Implements `IReadOnlyDictionary<TKey, TValue>`.

---

## Deprecated & Removed Collections (`WeakTable<K, V>`, `StaticMap<K, V>`)

Legacy collections `WeakTable<K, V>` and `StaticMap<K, V>` have been deprecated and retired:
- **`WeakTable<K, V>`**: Removed to prevent memory leaks and custom comparer hashcode mismatches. Use the BCL `System.Runtime.CompilerServices.ConditionalWeakTable<K, V>` for GC-safe weak-key associations.
- **`StaticMap<K, V>`**: Removed in favor of thread-safe BCL primitives like `ConcurrentDictionary<K, V>` or `FrozenDictionary<K, V>`.

---

## Composite Key (`CompositeKey`)

An immutable, hashable composite key struct for multi-dimensional caching and map indexing:

```csharp
var key1 = new CompositeKey(new object[] { "tenant_42", 1001, "read" });
var key2 = new CompositeKey(new object[] { "tenant_42", 1001, "read" });

bool areEqual = (key1 == key2); // true: structural element-wise equality
int hashCode = key1.GetHashCode(); // combined HashCode.Add over elements
```

- Implicit conversion from `object[]`.
- Element-wise structural equality using `object.Equals`.
- Optimized `HashCode` aggregation.

---

## Static String Dictionary (`StaticStringDictionary`)

Optimized dictionary lookup for frozen/static string keys, providing low-overhead indexer access for configuration dictionaries, headers, and protocol metadata.

