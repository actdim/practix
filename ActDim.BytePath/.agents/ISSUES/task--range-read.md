# range-read

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

Not a missing capability — a missing **promise**. `ReadAsync` already returns a seekable stream:
`FileStream` has `CanSeek == true`, so a caller can seek and read a range today. The contract says
nothing about it, which is the worst state, because callers will discover the behaviour and depend on
it. On S3 `GetObjectResponse.ResponseStream` has `CanSeek == false` and seeking throws
`NotSupportedException`, so that code breaks the moment a non-file-system backend appears.

Positioned reading is implementable on every backend — Azure's `OpenReadAsync` already returns a
seekable stream backed by range GETs — unlike positioned *writing*, which was dropped in #005 because
no object store can do it at all.

So this task has two separable halves, and the first is the one that matters now.

### Half 1 — state what a read stream guarantees (do this)

Document that `IBlobDataStore.ReadAsync` returns a **seekable** stream, and that a backend whose
native stream is forward-only must wrap it. Costs nothing today, makes existing behaviour
contractual, and covers resumable download immediately.

### Half 2 — explicit range overload (weak; only if a real need shows up)

The argument is **not** request count. A seek followed by one sized `Read` gives a wrapper both the
offset and the count, so it issues exactly one right-sized range GET — seeking is fine there.

The difference appears on repeated `Read` calls: consumers read in a loop (`CopyToAsync` at ~80 KiB a
time), and on each call the wrapper cannot know whether more reads follow or how far they go. It has
to pick a heuristic — a request per `Read` (bad for sequential reading), or one open-ended GET from
the seek position whose in-flight response a later seek tears down. An explicit window removes the
guess: one request of exactly the right length.

But for the motivating case — resumable download, i.e. seek then read to the end — the open-ended
heuristic is already optimal. So this half buys little and should wait for a concrete need rather
than being built on principle.

## Design (half 2)

```csharp
Task<Stream> ReadAsync(BlobRecord blobRecord, long offset, long? length, CancellationToken ct);
```

`length: null` means "to the end". Keep the existing parameterless `ReadAsync` as the common case
rather than forcing every caller to pass `0, null`.

Validate `0 <= offset <= size` against `GetSizeAsync` and clamp `length` to what remains, so a range
past the end is an explicit `ArgumentOutOfRangeException` instead of a silently empty stream. This is
the check that positioned writing could not have safely: here there is nothing to corrupt, only a
range to reject.

Requires a read lock, like `ReadAsync`.

`FileSystemBlobDataStore`: open as now, `Seek(offset)`, and bound the length — a wrapper limiting the
readable window is needed, since `FileStream` itself will happily read past `length`.

## Done when

Half 1 — **done 2026-08-06**:

- [x] `ReadAsync` documents that the returned stream is seekable, and that a forward-only backend
      must wrap it; noted in `AGENTS.md` alongside the write surface
- [x] `DataStore_ReadAsync_ReturnsSeekableStream` asserts `CanSeek` and a seek-then-read, pinning it

Half 2 (deferred):

- [ ] `IBlobDataStore` carries the overload and `FileSystemBlobDataStore` implements it
- [ ] range past the end throws rather than returning an empty stream
- [ ] tests: middle range, range to the end, offset at the end (empty), offset past the end (throws),
      length beyond the end (clamped), missing read lock (throws)
