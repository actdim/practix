---
date: 2026-08-04
slug: stream-extensions-hardening
agent: Claude Code / claude-opus-4-8
branch: main
commit: 258ed29
summary: Reviewed and hardened Extensions/StreamExtensions.cs (correctness + zero-alloc), migrated ReadBytes to IBufferOwner, added tests, renamed ToString to GetString.
---
# StreamExtensions hardening

## What & why
Full review and rework of `Extensions/StreamExtensions.cs` toward correctness and low GC / zero-alloc.

- **Partial reads fixed**: `ToString`/`ReadBytes` used a single `Stream.Read`, which is not guaranteed to fill the buffer; switched to `ReadExactly` / `ReadExactlyAsync`.
- **Broken `GetBuffer()` fallbacks removed (4 sites)**: after `TryGetBuffer() == false` the `MemoryStream` is non-exposable and `GetBuffer()` throws `UnauthorizedAccessException`; replaced with a pooled read / the new `PooledCopy` helper.
- **`ZeroAllocCopyTo(this Stream)` / async**: removed the `checked((int)Length)` 2 GB cap (threw `OverflowException`); both now loop over the full pooled buffer via shared `PooledCopy` / `PooledCopyAsync`. Also fixed a **pre-existing infinite recursion**: delegating `ms.ZeroAllocCopyTo(dst, bufferSize)` re-bound to the same `Stream` overload → stack overflow; the `MemoryStream` overloads now accept `bufferSize`.
- **`WriteString` / `WriteStringAsync`**: dropped `StreamWriter`; encode into a pooled buffer via `Encoding.GetBytes` (zero-alloc, no BOM); default `Utf8NoBom`; added `CancellationToken`; dropped the meaningless `bufferSize`.
- **`ReadBytes` / `ReadBytesAsync` → `IBufferOwner<byte>`** (ownership model): factory `Func<int, IBufferOwner<byte>>`, default `ArrayPoolBufferOwner.Rent`; fast path copies into the owner (safe against RecyclableMemoryStream reuse). Propagated the return type through `CompressionManager.*ToBytesAsync` (8 methods) and the `dstFactory` type.
- **`ToString` → `GetString`, `ToStringAsync` → `GetStringAsync`**: avoid the `object.ToString()` collision. Updated `EncodingExtensions`, `EntityFetcher`, and the tests.
- **`WriteSafe` / `WriteSafeAsync`**: added `Guard.Against.Null` + `NegativeOrZero` (chunkSize 0 was an infinite loop).
- Removed dead `WriteBuffered` / `WriteBufferedAsync` stubs; filled empty XML-doc summaries.

## Files touched
- `Extensions/StreamExtensions.cs` (main rework)
- `Extensions/EncodingExtensions.cs` (GetString call)
- `Compression/CompressionManager.cs` (IBufferOwner<byte> return type across 8 *ToBytesAsync methods)
- `../ActDim.Practix.DataAccess/EntityMapping/Fetch/EntityFetcher.cs` (GetString call)
- `../Tests/Common.Tests/Extensions/StreamExtensionsTests.cs` (new, ~44 tests)

## Verification
`dotnet test Common.Tests` → 151 passed / 0 failed. `ActDim.Practix.Common` builds clean.

## Known gaps / follow-ups
- `ReadBytes` `ownerFactory` contract: a custom factory MUST return an owner whose `Length` equals the requested length (the default `ArrayPoolBufferOwner.Rent` is correct).
- `ActDim.Practix.DataAccess` has pre-existing, unrelated build errors (`OrthoBits.Abstractions` namespace); the `GetString` rename is applied there but the project does not build for independent reasons.
