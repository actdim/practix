---
protocol: along
protocol_version: "2.2.18"
slug: extensions-and-utilities
title: Extensions & Common Utilities
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [extensions, streams, strings, guards, random-id, utilities]
---

# Extensions & Common Utilities

`ActDim.Practix.Common` provides an extensive suite of high-performance BCL extensions and utilities focused on zero-allocation I/O, guard clauses, string encoding, task combinators, and collision-resistant random identifiers.

---

## Stream Extensions (`StreamExtensions`)

High-performance stream helpers that leverage underlying buffer visibility (`TryGetBuffer`) and `ArrayPool<byte>.Shared`:

```csharp
// 1. Decode stream to string (zero copy for exposable MemoryStream, UTF-8 without BOM default)
string content = await stream.GetStringAsync();

// 2. Encode string directly into stream without allocating StreamWriter
int bytesWritten = await stream.WriteStringAsync("payload content");

// 3. Zero-allocation stream copying (writes directly from internal buffer when available)
await memoryStream.ZeroAllocCopyToAsync(destinationStream);

// 4. Copy whole stream into pooled RecyclableMemoryStream
using MemoryStream pooledStream = await sourceStream.ToMemoryAsync();

// 5. Read entire stream into pooled IBufferOwner<byte>
using IBufferOwner<byte> bufferOwner = await stream.ReadBytesAsync();

// 6. Write in bounded chunks with fine-grained cancellation checks
await destinationStream.WriteInChunksAsync(largeByteArray, chunkSize: 8192, ct);
```

---

## String Extensions (`StringExtensions`)

Zero-allocation string conversion and memory streaming:

```csharp
// Encode string directly into pooled MemoryStream
using MemoryStream stream = await "text payload".ToMemoryAsync();
```

---

## Task & Threading Extensions (`TaskExtensions`, `ThreadSafe`)

Extensions for task manipulation, timeout decoration, and cancellation handling:

- `WithCancellation(CancellationToken)`: Wraps an un-cancellable task with a cooperative cancellation token.
- `WithTimeout(TimeSpan)`: Wraps task with an async timeout deadline.
- `IgnoreCancellation()`: Safely suppresses `OperationCanceledException` when tasks are cancelled intentionally.

---

## Guard Clauses (`GuardExtensions`)

Fluent argument validation helpers:

```csharp
Guard.Against.Null(arg, nameof(arg));
Guard.Against.NullOrEmpty(str, nameof(str));
Guard.Against.NegativeOrZero(count, nameof(count));
```

---

## Cryptographic & Random Identifiers (`RandomId`)

Generates high-density, URL-safe, collision-resistant random identifiers using `RandomNumberGenerator`:

```csharp
// Base62 (default): a-z, A-Z, 0-9
string id1 = RandomId.Generate(12);

// Base58: removes visually ambiguous characters (0, O, I, l)
string id2 = RandomId.Generate(16, IdAlphabetType.Base58);

// Crockford Base32: human-readable base 32
string id3 = RandomId.Generate(10, IdAlphabetType.CrockfordBase32);

// Custom alphabet with uniqueness validation
string customId = RandomId.Generate(8, "ABCDEF123456");
```

---

## Type & Introspection Helpers (`Introspection`)

Provides structured reflection metadata caching (`IntrospectionInfo`, `MethodIntrospectionInfo`, `FieldIntrospectionInfo`) and `NameHelper` for sanitized member/type name resolution.

