---
slug: compression-large-payload-spill
status: open
created: 2026-08-04
updated: 2026-08-04
---
> Depends on `adaptive-stream`, which owns the memory→file spilling engine and the threshold/temp-directory/
> cleanup options. The two open questions below are answered there — but only if that task ships a writable
> (not just read-only) surface; see its first open question.
# Large-payload spill for CompressionManager

`CompressionManager.CreateTempStream()` always returns a memory-backed pooled stream
(`MemoryManager.Default.GetStream(...)`), so any path that has to materialize a payload is bounded by
`MaximumStreamCapacity` (currently 1 GB) and by RAM.

## Goal
A variant that spills to a temporary `FileStream` above a size threshold, without changing the public
contract (the hook already exists: `CreateTempStream` is `protected virtual`).

## Which paths need it
Only the ones that materialize rather than stream:
- `CompressAsync`/`DecompressAsync`/`CompressToArchiveAsync` overloads that RETURN a stream;
- `DecompressAsync(Stream) → byte[]` and the `*ToBytesAsync` family (bounded by `int.MaxValue` anyway);
- the non-seekable fallbacks (`Stream.ToMemoryAsync`) used for sniffing and for ZIP random access;
- TAR writing, which needs the entry length before the data.

The straight input→output overloads already stream and need nothing.

## Open questions
- Threshold + temp directory as `protected virtual` members, or an options object?
- Deleting the temp file on dispose (`FileOptions.DeleteOnClose`) vs. explicit cleanup on crash.
