---
protocol: along
protocol_version: "2.2.18"
slug: concurrent-collections
title: Specialized Concurrent Collections
type: topic
created: 2026-09-03
updated: 2026-09-03
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

## Weak Table (`WeakTable<K, V>`)

A concurrent weak table supporting **non-identity equality comparers**, modeled after .NET's `ConditionalWeakTable<K, V>`:

```csharp
var weakMap = new WeakTable<Document, DocumentMetadata>(customComparer);

weakMap.Add(doc, metadata);

if (weakMap.TryGetValue(doc, out var meta))
{
    // Access metadata
}
// When 'doc' is garbage collected, weakMap cleans up the entry automatically
```

### Mechanics:
- **Resurrection Tracking**: Uses `WeakReference<State>` to track keys through finalization.
- **Custom Comparer**: Unlike `ConditionalWeakTable` (which strictly uses reference identity `RuntimeHelpers.GetHashCode`), `WeakTable` supports custom `IEqualityComparer<K>`.
- **Cyclic Reference Safety**: Keys and values do not hold strong circular references, preventing memory leaks when a value holds a reference back to its key.

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

