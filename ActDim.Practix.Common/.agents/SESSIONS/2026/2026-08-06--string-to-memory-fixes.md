---
date: 2026-08-06
slug: string-to-memory-fixes
agent: Claude Code / claude-opus-5[1m]
branch: main
commit: 4995d1e
summary: >
  StringExtensions.ToMemory rewritten to encode straight into the stream instead of staging through a
  rented buffer, both ToMemory overloads now return a rewound stream, and WriteSafe renamed to
  WriteInChunks using the shared BufferSize.
---

# ToMemory: one copy instead of two, and a consistent stream position

Small, review-driven session. The assembly was mid-refactor and not compiling when it started (missing
usings in `StringExtensions`, `StreamExtensions.BufferSize` private while used as a public default,
`IBufferOwner<>` moved namespace); the build was fixed by the user before these changes.

## `StringExtensions.ToMemory`

It rented a buffer from `ArrayPool`, encoded into it, then passed it to
`MemoryManager.Default.GetContextStream(buffer, 0, length)`. That is safe: `RecyclableMemoryStreamManager`
copies the buffer into its own blocks rather than wrapping it, so returning the array to the pool in the
`finally` is not a use-after-return. But it means the bytes are written **twice**: once into the staging
buffer, once into the stream's blocks.

Restored the approach that had been left commented out in the file: `WriteString` encodes directly into
the pre-sized stream, so there is one pooled rent (inside `WriteString`) and one write. The commented
block could not have compiled as written: it called `WriteString(value, encoding, bufferSize)` and no
such overload exists: so it was stale rather than a working alternative, and has been deleted.

## Stream position was inconsistent between the two overloads

`GetContextStream(buffer, offset, count)` hands back a stream positioned at 0, whereas the `WriteString`
path leaves it at the end. So `ToMemory` and `ToMemoryAsync`: the same operation, sync and async -
returned streams in different states. Both now rewind before returning, which is what a caller of
something named `ToMemory` wants; the pre-existing `stream.Position = 0L` before the write was a no-op on
a fresh stream and moved after it.

This benefits the only caller of the string overloads, `EncodingExtensions.GetStreamAsync`: a stream
obtained from a string can now be read without rewinding it by hand.

`bufferSize` was dead in `ToMemory` (nothing in the body used it) and has been removed, following the
same in-place comment convention already used on `StreamExtensions.ToMemory`.

## `WriteSafe` → `WriteInChunks`

Renamed at the user's request, together with `WriteSafeAsync` → `WriteInChunksAsync`; "safe" did not say
what it protected against, and the `Guard` calls in the body actively misled toward null-safety. The
hard-coded `chunkSize = 8192` now uses the shared `BufferSize` constant, as the neighbouring
`ZeroAllocCopyTo*` methods and the predecessor `WriteBlocks` did.

Extended their XML docs with two facts that were previously nowhere: these helpers are unnecessary for a
plain `FileStream` or `MemoryStream`, where a single `Write(data, 0, data.Length)` is equivalent; and in
the async form the chunk size sets the **cancellation granularity**, since one large `WriteAsync` may not
observe the token until it completes.

## Files touched

- `Extensions/StringExtensions.cs`: `ToMemory`, `ToMemoryAsync`, unused `using System.Buffers` removed
- `Extensions/StreamExtensions.cs`: rename, `BufferSize`, docs
- `Tests/Common.Tests/Extensions/StreamExtensionsTests.cs`: renamed call sites (276 passing)

## Gaps / follow-ups

- The string `ToMemory` / `ToMemoryAsync` have **no tests at all**: the existing `ToMemory` tests cover
  the `Stream` overloads. The stream position is now a documented behaviour with nothing pinning it.
- Two warnings unrelated to this work: `Introspection/TypeBaseIntrospectionInfo.cs:30` (redundant `new`)
  and `Memory/ArrayPoolBufferOwner.cs:33` (`?` outside a nullable context).
