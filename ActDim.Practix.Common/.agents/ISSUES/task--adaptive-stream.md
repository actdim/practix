---
slug: adaptive-stream
status: open
created: 2026-08-04
updated: 2026-08-04
---
# AdaptiveStream — read-only stream that picks memory or disk by size

A read-only, seekable `Stream` over temporary immutable data that stores small payloads in memory and
spills large ones to a temporary file, so callers never choose between `MemoryStream` and `FileStream`.

Spec supplied by the user (2026-08-04); this file is the digested version plus the points that must be
settled before coding.

## Goal
`ActDim.Practix.Common` gains a materialization primitive with bounded memory use:
- one `Stream` abstraction, `CanRead`/`CanSeek` true, `CanWrite` false, `Length` always known after creation;
- data under a threshold stays in pooled memory (fast path unchanged for the common case);
- data over it goes to a temp file, so multi-GB payloads work at constant memory;
- a source stream of unknown length starts in memory and hands over to a file mid-copy.

Intended consumers: compression/decompression, archive processing (ZIP random access needs a seekable
copy), HTTP payloads, temporary processing pipelines.

## Non-goals (from the spec)
Not writable to consumers, not permanent storage, not a cache, not a blob store, not a `FileStream`
replacement. Content is immutable after creation.

## Why now — relation to existing tasks
- `compression-large-payload-spill` — **read the first open question below before assuming this closes it.**
  The read-only surface covers the `Stream.ToMemoryAsync` fallbacks (sniffing, ZIP random access), but NOT
  the `CreateTempStream()` sites that write into the scratch stream
  (`Compression/CompressionManager.cs` — ~13 call sites, incl. the TAR writer's length-before-data need).
- Today every materialization goes through `MemoryManager.Default`, so it is bounded by
  `MaximumStreamCapacity` (1 GB) and by RAM.
- `compression-interface-cleanup` is independent; no ordering constraint.

## Public surface (as specced)
```csharp
public sealed class AdaptiveStream : Stream
{
    public static ValueTask<AdaptiveStream> CreateAsync(Stream source, AdaptiveStreamOptions? options = null,
        CancellationToken cancellationToken = default);
    public static AdaptiveStream Create(ReadOnlyMemory<byte> data, AdaptiveStreamOptions? options = null);
    public static AdaptiveStream Create(ReadOnlySpan<byte> data, AdaptiveStreamOptions? options = null);
    public static ValueTask<AdaptiveStream> CreateFromFileAsync(string fileName,
        AdaptiveStreamOptions? options = null, CancellationToken cancellationToken = default);

    public bool IsMemoryBacked { get; }
    public bool IsFileBacked { get; }
    public string? TemporaryFilePath { get; }   // diagnostics only; consumers must not branch on backing
}

public sealed class AdaptiveStreamOptions
{
    public long MemoryThreshold { get; init; } = 4 * 1024 * 1024;
    public string? TemporaryDirectory { get; init; }          // null -> system temp
    public bool DeleteTemporaryFileOnDispose { get; init; } = true;
    public int CopyBufferSize { get; init; } = 128 * 1024;
}
```
Semantics: `<= MemoryThreshold` memory, `> MemoryThreshold` file. `Read`/`ReadAsync`/`Seek`/`Position`/
`Length` supported; `Flush` no-op; `Write`/`SetLength` throw `NotSupportedException`. Ownership: the
instance owns its inner stream, the caller keeps owning `source`. Thread safety: standard `Stream` rules —
not thread-safe, concurrent reads need external synchronization.

## Decisions needed before coding

1. **Writable-then-sealed mode, or read-only only?** The spec's "unknown length" flow already IS a
   spilling writable sink; it is simply not exposed. Options:
   (a) read-only only, as specced — `compression-large-payload-spill` then needs a *second*, separate
   spilling writable stream, and we own two implementations of the same handover logic;
   (b) add an internal/`protected` writable phase plus a seal step (e.g. `CreateWritable()` →
   `SealAsync()` returning the read-only `AdaptiveStream`), and make `CreateTempStream()` use it — one
   engine, but the "never exposes writable state" design principle is relaxed to "not after sealing";
   (c) factor the handover into an internal `SpillingBuffer` used by both surfaces.
   Recommendation: (c) — keeps the public spec intact and still unblocks the spill task. Needs a call.
   Note `CreateTempStream()` is **synchronous**, so the writable path must not require async creation.

2. **Memory backing: pooled or plain `MemoryStream`?** The spec says `MemoryStream`; the repo standard is a
   pooled `RecyclableMemoryStream` from `MemoryManager.Default` (see GLOSSARY "Temp / scratch stream").
   Pooled is the right default here, with two consequences to document: the manager is configured with
   `ThrowExceptionOnToArray`, and its `MaximumStreamCapacity` is 1 GB — harmless while the threshold is
   4 MB, but the threshold must be validated against it (and against `int.MaxValue`) instead of failing deep
   inside the copy.

3. **`CreateFromFileAsync` — copy or open in place?** Copying a 20 GB file to temp is absurd; opening the
   existing file read-only is what callers want. Then `DeleteTemporaryFileOnDispose` must NOT touch it, and
   `TemporaryFilePath` should stay `null` (the file is not ours). That means a third internal state,
   "foreign file", distinct from "temp file". Confirm, and confirm what a *small* file does — still open in
   place, or read into memory (threshold applies)?

4. **`Create(ReadOnlyMemory<byte>)` vs `Create(ReadOnlySpan<byte>)`.** The span overload must copy. If the
   memory overload also copies, the pair is duplicated surface for no gain; if it wraps zero-copy, we need
   our own `ReadOnlyMemory<byte>`-backed stream (the BCL has no public one) and the caller must promise not
   to mutate the buffer — which contradicts "temporary immutable data". Pick one: drop the memory overload,
   or make it the documented zero-copy one.

5. **`ValueTask<T>` on the factories.** No synchronous completion path and no pooling, so `ValueTask` buys
   nothing over `Task` and costs the usual single-await footgun. Suggest `Task<AdaptiveStream>` unless there
   is a reason to keep it.

6. **Source ownership knob.** The spec mentions a possible `leaveOpen: false` overload. Prefer
   `bool LeaveSourceOpen { get; init; } = true;` on the options object over an overload matrix.

7. **`CopyBufferSize = 128 KB` contradicts an existing deliberate choice.** `CompressionManager.BufferSize`
   is 81920 specifically to stay under the LOH threshold. Either default to 81920 for consistency, or keep
   128 KB and rent from `ArrayPool` (which caches to 1 MB, so LOH is a non-issue) — but say which and why.
   Also note the value is ignored for pooled-memory copies (`RecyclableMemoryStream.CopyTo(Async)` ignores
   `bufferSize`); it only matters on the file path.

8. **Temp file cleanup mechanism.** `FileOptions.DeleteOnClose` covers dispose *and* process kill on
   Windows, but it is incompatible with `DeleteTemporaryFileOnDispose = false` and makes the path
   un-openable by anyone else — so `TemporaryFilePath` becomes useless in that mode. Likely: `DeleteOnClose`
   when the flag is true, explicit `File.Delete` in a `finally`/`Dispose` when false is not requested.
   Decide, and decide who owns the file when the flag is false (the caller, documented).

9. **Two bools or one enum** for `IsMemoryBacked`/`IsFileBacked` (they cannot both be true). Cosmetic; an
   enum is harder to misread, the bools match the spec.

10. **Placement.** Existing folders are `Compression/`, `Extensions/`, `Memory/`. This is neither
    compression nor an extension — `Memory/AdaptiveStream.cs` or a new `IO/`. Pick before creating files.

## Implementation notes
- Known-length source above the threshold: go straight to the file, skip the memory phase entirely.
- Unknown-length source: buffer to memory, and on crossing the threshold create the temp file, copy what is
  already buffered, then continue into the file. Byte order across the handover is the thing to test.
- File mode: `FileOptions.Asynchronous` for the async path; `SequentialScan` is a *hint that can hurt* here
  because consumers seek — leave it off or justify it. Consider `bufferSize: 0/1` to avoid double buffering
  since we copy in large chunks ourselves. After filling, rewind to 0 and serve reads from the same handle.
- Override the modern surface, not just the legacy one: `Read(Span<byte>)`, `ReadAsync(Memory<byte>, …)`,
  `ReadByte`, and `CopyToAsync` (delegate to the inner stream so pooled/file fast paths survive). Implement
  `DisposeAsync`, and make dispose idempotent.
- Failure/cancellation at any point during creation: dispose the inner stream, delete the temp file, do not
  touch `source`, let the original exception surface.

## Acceptance criteria
- Threshold boundary: exactly `MemoryThreshold` → memory; `+1` → file (for known- and unknown-length
  sources; known-length-above-threshold never allocates the memory phase).
- Unknown-length source crossing the threshold mid-copy yields byte-identical content in the right order.
- `Read`/`ReadAsync`/`Seek` (all `SeekOrigin` values)/`Position`/`Length` behave as a normal seekable stream;
  `Write`/`SetLength` throw `NotSupportedException`; `Flush` is a no-op; `CanWrite` is false.
- Temp file is gone after `Dispose`/`DisposeAsync`, and also after a mid-creation exception or cancellation.
- `source` is left open by default and disposed only when ownership is transferred.
- `CreateFromFileAsync` never deletes the caller's file.
- Empty source → memory-backed, `Length == 0`.
- Large-payload smoke test (well above threshold, opt-in / skippable so CI stays fast).
- Tests live in `Tests/Common.Tests` alongside the existing suites; the 268 passing tests stay green.

## Follow-ups (not this task)
- Rewire `CompressionManager.CreateTempStream()` / the `ToMemoryAsync` fallbacks onto this —
  `compression-large-payload-spill`.
- Add "adaptive stream" to `.agents/GLOSSARY.md` once the shape is fixed.
- Record the outcome of the open questions above in `.agents/DECISIONS.md` (memory backing, ownership,
  writable mode).
