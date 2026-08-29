---
slug: compression-interface-cleanup
status: open
created: 2026-08-04
updated: 2026-08-04
---
# Compression interface cleanup

`ICompressionManager` + `IArchiveEntry` / `IArchiveInfo` / `ArchiveEntrySource`
(`ActDim.Practix.Abstractions/Compression/`) are now fully implemented by
`ActDim.Practix.Common/Compression/CompressionManager.cs`. Writing that implementation surfaced a list of
contract problems. Nothing was changed in the abstraction: that is a deliberate, separate decision.

## Why now
`grep` over the whole repo: the ONLY reference to `ICompressionManager` outside the abstraction itself, the
implementation and its tests is the Autofac registration in `ActDim.Practix.Common/CommonModule.cs`. There are
zero call sites. Every change below is therefore free today (implementation + tests only) and stops being free
the moment the first consumer appears.

Legend: **P** = problem, ordered by value, not by size. "Workaround" = what `CompressionManager` does today.

---

## P1: `Task<byte[]> DecompressAsync(Stream, …)` is the odd one out

```csharp
Task<Stream> DecompressAsync(ReadOnlyMemory<byte> data, CompressionFormat? = default, CancellationToken = default);
Task<byte[]> DecompressAsync(Stream  stream,           CompressionFormat? = default, CancellationToken = default);
```

**Problems.** Three in one signature:
1. It is the single member of the whole interface that is *forced* to allocate on the managed heap, which
   contradicts the goal the rest of the type is built around.
2. It is asymmetric with its own sibling: `data` in → `Stream` out, but `Stream` in → `byte[]` out. A caller
   cannot express "stream in, stream out" through the returning form at all, only through the
   `outputStream` form.
3. `byte[]` caps the result at ~2 GB. The implementation has to throw `NotSupportedException` for a payload
   above `int.MaxValue`, even though the decompression pipeline itself has no such limit.

**Workaround.** Implemented literally (`GC.AllocateUninitializedArray` + `ReadExactly`, so at least the
zeroing pass is skipped), plus `DecompressToBytesAsync` was added *outside* the interface returning a pooled
`IBufferOwner<byte>`.

**Options.**
- (a) Interface gets `Task<Stream> DecompressAsync(Stream, …)` for symmetry, and the byte-oriented shape moves
  to `Task<IBufferOwner<byte>> DecompressToBytesAsync(…)`: both already exist on the implementation.
  **Recommended.**
- (b) Keep a `byte[]` convenience but rename it `DecompressToArrayAsync`, so it reads as an explicit
  "materialize it for me" choice rather than the default way to decompress a stream.

**Blocker for (a):** `IBufferOwner<T>` currently lives in `ActDim.Practix.Common/Extensions/IBufferOwner.cs`
(namespace `ActDim.Practix.Extensions`), i.e. in the *implementation* project. `ActDim.Practix.Abstractions`
cannot reference it (the dependency runs Common → Abstractions). Putting a pooled-buffer return type into the
interface therefore requires moving `IBufferOwner<T>` into Abstractions first. Decide that before P1.

---

## P2: There is no way to compress INTO a stream the caller owns

Decompression has both forms; compression has neither:

```csharp
Task DecompressAsync(ReadOnlyMemory<byte> data,   Stream outputStream, …);   // exists
Task DecompressAsync(Stream stream,               Stream outputStream, …);   // exists
// nothing equivalent for CompressAsync
```

**Problem.** Compressing into an already-open destination is the common real case: an HTTP response body, a
file, a blob upload, an entry inside an outer container. With only `Task<Stream> CompressAsync(…)` the caller
must accept a scratch stream and then copy it into the real destination: one extra full copy plus a pooled
stream round-trip per operation, exactly the cost the design is trying to avoid. Note the contract is
inconsistent with *itself*: `CompressToArchiveAsync` **does** have `outputStream` overloads.

**Workaround.** `CompressionManager` declares the two missing overloads as public methods beyond the
interface; the interface-declared `Task<Stream>` ones are thin wrappers over them.

**Option.** Promote both into `ICompressionManager`. Mechanical, no implementation work left.

---

## P3: The detection API cannot express "unknown", cannot go async, and is impossible for 2 of 3 formats

```csharp
ArchiveFormat     GetArchiveFormat(Stream stream);
CompressionFormat GetCompressionFormat(Stream stream);      // + ReadOnlyMemory<byte> overloads
```

**P3a: no `Unknown` member.** Both enums start at their first real value and have no `None`/`Unknown`
(the legacy `CompressionType` did have `None`). "Is this data compressed?" is an ordinary control-flow
question, but the only way to answer it through this API is to catch `System.IO.InvalidDataException`.
Exceptions as normal flow, and expensive at that.

**P3b: sync signature, async problem.** Sniffing means "read the first bytes, then un-read them", which needs
a seekable stream. For a network stream / pipe the only correct move is to buffer it: which is inherently
async and cannot be done from a sync method. So `GetCompressionFormat(nonSeekableStream)` can only throw
`NotSupportedException`. Internally `DecompressAsync` *does* handle this (buffers via
`Stream.ToMemoryAsync`), so the capability exists but is unreachable through the public detection API.

**P3c: the default value cannot work for most formats.** Every decompress overload defaults
`compressionFormat` to `null` = "detect it". But Brotli and raw Deflate are headerless by design and can never
be detected: 2 of the 3 formats actually supported. So `await manager.DecompressAsync(brotliBytes)` compiles,
reads as the obvious call, and always throws. The API shape advertises a capability that does not exist.

**Options.**
- `bool TryGetCompressionFormat(ReadOnlyMemory<byte> data, out CompressionFormat format)` (+ archive twin) for
  the non-throwing question, and/or `Unknown` enum members.
- `Task<CompressionFormat?> DetectCompressionFormatAsync(Stream stream, CancellationToken ct)` for the
  non-seekable case (buffers internally, exactly like the decompress path already does).
- Make `compressionFormat` **required** on the decompress overloads so auto-detection becomes explicit opt-in
  (`DecompressDetectedAsync`, or passing `null` deliberately) instead of the accidental default.
  Detection helpers already exist privately in the implementation (`TryDetectCompressionFormat` /
  `TryDetectArchiveFormat`) and were kept private only because the interface has no such shape.

---

## P4: `ArchiveFormat?` / `CompressionFormat?` null means two different things

| Method | `null` means |
|---|---|
| `DecompressAsync`, `DecompressArchiveAsync`, `GetArchiveEntriesAsync` | detect from the content |
| `CompressToArchiveAsync`, `FixArchiveFileExtension` | use the implementation default (ZIP) |

**Problem.** Identical spelling, identical type, opposite semantics: and the difference is invisible at the
call site. On the write side "detect" is meaningless (there is nothing to detect yet), so the overloading of
the same idiom is silent. A reader of `CompressToArchiveAsync(sources)` has no way to know whether that
produces a ZIP, or fails, or infers something.

**Options.** Make the format required on the write methods; or add an explicit `Auto`/`Detect` enum member and
drop the nullability; or at minimum rename the parameters (`detectedArchiveFormat` vs `targetArchiveFormat`)
and document both on the interface.

---

## P5: `GetArchiveFormatByFileExtension` throws for anything it does not know, and has no compression twin

```csharp
ArchiveFormat GetArchiveFormatByFileExtension(string ext);
```

**Problems.**
1. No `TryGet`, no `Unknown` → classifying arbitrary user file names requires try/catch around every call
   (`.txt`, `.gz`, `.pdf` are all `NotSupportedException`). Same disease as P3a.
2. Ambiguous input contract: is `ext` `".zip"`, `"zip"`, or a whole file name? (The implementation accepts the
   first two and trims a leading dot; a full name is rejected.)
3. Two-part extensions are unrepresentable: for `backup.tar.gz`, `Path.GetExtension` yields `.gz`, which is a
   `CompressionFormat`, not an `ArchiveFormat`. The honest answer is a *pair* (TAR container + GZip outer
   codec) and the return type cannot express it. The implementation special-cases the `.tgz`/`.tbz2`/`.txz`
   shorthands as TAR, which is a partial answer only.
4. There is no `GetCompressionFormatByFileExtension` / `FixFileExtension` counterpart, although the legacy
   commented-out code had both. So `.gz` / `.br` cannot be mapped to a `CompressionFormat` through this
   interface at all.

**Options.** `bool TryGetArchiveFormatByFileName(string fileName, out ArchiveFormat archive, out CompressionFormat? outerCodec)`,
plus the missing compression-format helpers; accept full file names, not just extensions.

---

## P6: `FixArchiveFileExtension` has undocumented semantics

```csharp
string FixArchiveFileExtension(string fileName, ArchiveFormat? archiveFormat = default);
```

**Problem.** "Fix" does not say whether a wrong extension is **appended to** or **replaced**. The
implementation appends (`data.bin` → `data.bin.zip`, never losing part of the original name), returns the same
string instance when it is already correct, and accepts `.tgz` / `.tar.gz` as already-correct for TAR: all
reasonable, all invisible to someone reading only the interface.

**Options.** Document the rule on the interface member, or split it into explicit `EnsureExtension` /
`ReplaceExtension`.

---

## P7: `ArchiveEntrySource` is under-specified

```csharp
public class ArchiveEntrySource
{
    public string FullName { get; init; }
    // public long Size { get; set; }        <- commented out in the source
    public Func<Stream> OpenReadAsync { get; init; }
}
```

**Problems.**
1. `OpenReadAsync` is named `…Async` but is a **synchronous** `Func<Stream>`. Opening a real source (file,
   blob, HTTP) is I/O; the type forces it to block.
2. No `CancellationToken` reaches the opener.
3. **No way to declare a known length.** TAR must write the entry size into the header *before* the data, so a
   non-seekable source has to be fully buffered into a scratch stream first. If the caller knows the length
   (they usually do), that buffering is pure waste. The commented-out `Size` property shows the gap was
   already noticed.
4. Ownership is unspecified: who disposes the stream the delegate returned? The implementation disposes it
   (we asked for it, so we close it): defensible, but the type says nothing, and the opposite assumption
   leaks file handles.
5. No validation: `FullName` may be null/blank and is only rejected deep inside the write loop, i.e. after
   part of the archive was already written.

**Options.** `Func<CancellationToken, ValueTask<Stream>> OpenRead`, optional `long? Length`, documented
ownership, and validation at construction (or make it a `record` with a validating primary constructor).

---

## P8: `IArchiveEntry` / `IArchiveInfo` are mutable bags with a circular reference

```csharp
public interface IArchiveEntry { string FullName { get; set; } long Size { get; set; } IArchiveInfo ArchiveInfo { get; set; } }
public interface IArchiveInfo  { string FileName { get; set; } long Size { get; set; } ICollection<IArchiveEntry> Entries { get; set; } }
```

**Problems.**
1. **Setters that do nothing.** In a writer callback a caller can set `entry.FullName`: and it is silently
   ignored, because the container entry must be created before its stream can be opened. Both formats now
   behave identically (the name is captured before the callback; asserted by
   `CompressToArchive_WithWriter_RenamingEntryInCallback_HasNoEffect`), but a silent no-op is worse than a
   compile error. Before that fix, ZIP ignored the rename and TAR honoured it: the mutable contract made two
   equally defensible readings possible.
2. **`Size` has no single meaning.** Reading: uncompressed size. Creating: nothing: the interface's own XML
   doc says "not valid in Create mode ... only valid in Update mode if the entry has not been opened", which
   is a description of `System.IO.Compression`'s implementation detail leaking into a format-agnostic
   abstraction. The implementation fills it post-hoc in the writer path (bytes actually written) and leaves it
   0 otherwise.
3. **`ArchiveInfo.FileName` is meaningless** for a stream-backed archive: always null in practice, since the
   whole API takes streams and `ReadOnlyMemory`, never paths.
4. **`ArchiveInfo.Entries` is format-dependent.** ZIP is random access, so the list is complete before the
   first callback. TAR is sequential, so it only ever contains the entries seen so far. A callback reading
   `entry.ArchiveInfo.Entries.Count` gets different answers per format: an observable behavioural difference
   that the abstraction promises to hide.
5. The back-reference makes the object graph circular (`info.Entries[i].ArchiveInfo == info`), which is
   awkward for serialization/logging and forces one allocation per entry (entries cannot be pooled or reused,
   because a callback may legitimately keep the reference).

**Options.** A read-only shape for reading (`sealed record` or get-only interface), a separate descriptor for
writing (`ArchiveEntrySource` already is one), drop the back-reference or replace it with what callers
actually need, and specify `Size` as "uncompressed size, or -1 when unknown".

---

## P9: `IArchiveEntry` was missing the metadata archives actually carry: **DONE (read side), 2026-08-04**

**Was.** No entry type / `IsDirectory`, no `LastWriteTime`, no `CompressedSize`.

**Why it was functional, not cosmetic.** Directory entries exist in both supported formats (ZIP: a name ending
in `/`; TAR: `TarEntryType.Directory`) and were surfaced as ordinary entries, so a caller could not tell a
directory from an empty file: for ZIP an empty stream, for TAR a `null` stream. "Extract this archive to disk"
- the most common archive operation there is: was therefore **not implementable** on top of the interface: the
directory tree could not be recreated, empty directories were lost, timestamps could not be restored.

**What was added** to `IArchiveEntry` (+ `ArchiveEntry`, + populated by `CompressionManager` for both formats):
- `ArchiveEntryType EntryType`: new enum in Abstractions: `RegularFile` (default) / `Directory` /
  `SymbolicLink` / `HardLink` / `Other`. ZIP has no type field, so a directory is recognized by the empty
  file-name part (`ZipArchiveEntry.Name`), which is the same rule the BCL's own extraction uses; TAR maps from
  `TarEntryType` (`RegularFile`/`V7RegularFile`/`ContiguousFile` → `RegularFile`; devices, FIFOs, sparse and
  PAX/GNU pseudo entries → `Other`).
- `DateTimeOffset? LastWriteTime`: from `ZipArchiveEntry.LastWriteTime` / `TarEntry.ModificationTime`.
- `long? CompressedSize`: `ZipArchiveEntry.CompressedLength`; **null for TAR**, which stores its entries
  verbatim (a `.tar.gz` compresses the container, not the entries).
- `string LinkTarget`: `TarEntry.LinkName` for link entries, null otherwise (ZIP has no portable link form).
  Without it the two link entry types would be unactionable.

Entry construction was also de-duplicated into two mappers (`CreateEntry(ZipArchiveEntry…)` /
`CreateEntry(TarEntry…)`), replacing four copies of the same object initializer.

**Format asymmetry found while testing, now documented on the interface:** a ZIP timestamp is a timezone-less
DOS wall clock with 2-second resolution: only the wall-clock value is meaningful, and the offset that comes
back is the *reading machine's* local one. TAR/PAX stores a Unix timestamp and round-trips exactly as UTC.
`GetArchiveEntries_Zip_ReportsDirectoryTimestampAndCompressedSize` asserts the wall clock, not the offset.

**Still open: the write side, and it belongs to P7.** `ArchiveEntrySource` has only `FullName` +
`OpenReadAsync`, so directories, links and timestamps **cannot be produced**, only read. The read/write pair is
now deliberately lopsided: extraction works, faithful re-packing does not. The new tests build their
directory/link archives with the BCL directly for exactly this reason. When P7 is done, `ArchiveEntrySource`
needs `EntryType`, `LastWriteTime` and `LinkTarget` to match.

---

## P10: `OpenStreamDelegate` is sync and cancellation-blind

```csharp
public delegate Stream OpenStreamDelegate();
```

**Problem.** .NET 10 added `ZipArchiveEntry.OpenAsync(CancellationToken)`; a sync delegate cannot use it, so
the reader path opens ZIP entries synchronously even though the whole surrounding pipeline is async. Opening
an entry is also a point where cancellation would be useful and cannot be observed.

**Option.** `delegate ValueTask<Stream> OpenEntryStreamDelegate(CancellationToken ct)`. Note the ownership
rule stays as documented on the implementation: the manager closes the entry stream when the callback returns,
and it may be opened once per entry (`InvalidOperationException` otherwise: mandatory for ZIP, where an entry
is only finalized when its stream closes).

---

## P11: `Task<IList<IArchiveEntry>>` forces full materialization

**Problem.** For TAR, listing entries walks the entire archive (it is a sequential format) with no way to stop
early: "what is the first entry?" costs a full pass. For ZIP the list is cheap, so the same signature hides
wildly different costs. There is also no streaming form, so a 100k-entry archive must be materialized as a
list before the caller sees anything.

**Option.** `IAsyncEnumerable<IArchiveEntry> GetArchiveEntriesAsync(…)` as the primary shape (keep a
`ToListAsync`-style convenience). This also dissolves P8.4: with a pull-based enumeration nobody expects a
complete `ArchiveInfo.Entries` collection.

---

## P12: No per-call compression level, no ZIP entry-name encoding

**Problem.** Neither is anywhere in the contract, so `CompressionManager` exposes them as `protected virtual`
members (`DefaultCompressionLevel`, `DefaultArchiveFormat`, `BufferSize`). Consequences:
1. Choosing `Fastest` for a hot path and `SmallestSize` for cold storage requires two subclasses and two DI
   registrations: one injected `ICompressionManager` cannot do both.
2. `ZipArchive`'s `entryNameEncoding` is unreachable, so legacy ZIPs with CP866 / CP1251 entry names can
   neither be read nor produced. This is a real interop scenario, not a hypothetical.

**Option.** An optional options parameter (`CompressionOptions { CompressionLevel Level, Encoding EntryNameEncoding, … }`,
`null` = instance defaults). Keep the `protected virtual` members as the source of those defaults.

---

## P13: Smaller contract nits
- The delegates are nested in the interface, so every call site spells
  `ICompressionManager.ArchiveEntryReaderAsyncDelegate`. Moving them to the namespace would read better.
- `Task<bool>` as "continue / stop" is untyped and undocumented on the delegate itself; a two-member enum, or
  at least an XML doc, would remove the coin-flip at the call site.
- "The caller must dispose the returned stream" is invisible at the type level on every `Task<Stream>` member -
  currently only an XML-doc promise.

---

## P14: `ActDim.Practix.Common.InvalidDataException` is a landmine
Not part of the compression contract, but it bit this work directly. `ActDim.Practix.Common/InvalidDataException.cs`
declares an **empty** `public class InvalidDataException : Exception`: no message constructor, derived from
`Exception` rather than `IOException`. Inside any `ActDim.Practix.Common.*` namespace the short name resolves
to *it* rather than to `System.IO.InvalidDataException`, which silently changed the meaning of 5 tests until
both sides were fully qualified.

**Options.** Delete it (nothing uses it), or give it real constructors and a documented purpose. Either way,
stop leaving a same-named shadow of a common BCL exception in a namespace everything imports.

---

## Suggested order
1. ~~**P9**~~: **done 2026-08-04** for reading; the write-side half moved into P7.
2. **P1 + P2**: allocation and symmetry; the methods already exist, only the declarations move. Settle the
   `IBufferOwner<T>` location (P1 blocker) first.
3. **P3 + P4**: the detection footguns, in one pass, since both touch the same parameters.
4. **P7 + P8 + P10**: the entry/source model, now including the write-side metadata P9 left behind
   (`EntryType` / `LastWriteTime` / `LinkTarget` on `ArchiveEntrySource`). The largest design change; do it as
   one piece, not in slices.
5. **P5 + P6**: the file-name helpers.
6. **P11**, **P12**, **P13**, **P14**: as capacity allows.

## Decisions needed before coding
- Do we accept breaking `ICompressionManager` now (free: no consumers) or freeze it as published API?
- Does `IBufferOwner<T>` move to `ActDim.Practix.Abstractions`? Required for any pooled-buffer return type in
  the interface, and it affects `StreamExtensions.ReadBytes*` too.
- Do we keep the abstraction BCL-shaped (ZIP + TAR + GZip/Deflate/Brotli, per DECISIONS #001) or design the
  contract for a future third-party codec (BZip2/LZMA/7z/RAR)? That answer decides whether the unsupported
  enum members stay in `CompressionFormat` / `ArchiveFormat` at all.

## Blast radius
`ActDim.Practix.Abstractions/Compression/*`, `ActDim.Practix.Common/Compression/*`
(`CompressionManager`, `ArchiveEntry`, `ArchiveInfo`), `Tests/Common.Tests/Compression/CompressionManagerTests.cs`
(276 tests currently green), and the registration in `ActDim.Practix.Common/CommonModule.cs`. No other code in
the repo touches any of it.
