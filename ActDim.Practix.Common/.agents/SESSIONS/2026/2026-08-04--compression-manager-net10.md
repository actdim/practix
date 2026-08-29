---
date: 2026-08-04
slug: compression-manager-net10
agent: Claude Code / claude-opus-5[1m]
branch: main
commit: 5901ab9
summary: Implemented CompressionManager on the .NET 10 BCL (async ZipArchive, System.Formats.Tar, GZip/Deflate/Brotli) with pooled buffers and zero-alloc paths; added ArchiveEntry/ArchiveInfo and 117 tests.
---
# CompressionManager on .NET 10

## What & why
`Compression/CompressionManager.cs` was a stub: every `ICompressionManager` member threw
`NotImplementedException` and ~700 lines of pre-`ICompressionManager` code sat commented out. Implemented
the whole contract against the .NET 10 BCL only, keeping the payload off the managed heap.

- **Codecs**: `GZipStream` / `DeflateStream` (raw RFC 1951) / `BrotliStream`. No `BufferedStream` wrapper:
  those streams already buffer internally, so wrapping them (as the legacy code did) only added an
  allocation plus a copy per block. `CompressionLevel` moved to a `protected virtual` property because the
  interface carries no level (Autofac resolves the parameterless ctor, so no ctor parameter).
- **Archives**: .NET 10's new async ZIP API (`ZipArchive.CreateAsync`, `ZipArchiveEntry.OpenAsync`,
  `IAsyncDisposable`) and `System.Formats.Tar` (`TarReader`/`TarWriter`, PAX). BZip2/LZMA/LZMA2/PPMd/7z/RAR
  are *detected* by signature but throw `NotSupportedException`: see DECISIONS #001.
- **Detection**: magic-byte sniffing into a `stackalloc` span (265 bytes: TAR's `ustar` magic sits at
  offset 257), signatures as `static ReadOnlySpan<byte>` properties (static data, no heap array), original
  stream position restored. Brotli and raw Deflate are headerless and therefore undetectable by design.
- **Zero-alloc plumbing**: temp streams from `MemoryManager.Default` (RecyclableMemoryStream); copies via
  `StreamExtensions.ZeroAllocCopyToAsync` on the `MemoryStream` fast path and `Stream.CopyToAsync`
  (ArrayPool-backed) otherwise; `MemoryMarshal.TryGetArray` exposes a `ReadOnlyMemory<byte>` as a
  `MemoryStream` with no copy; `GC.AllocateUninitializedArray` for the one `Task<byte[]>` member;
  `Path.GetExtension(ReadOnlySpan<char>)` + span comparisons in the extension helpers (no `ToLower`);
  entry openers are one object + one delegate per archive instead of a closure per entry; `for` over
  `ZipArchive.Entries` instead of `foreach` (no boxed enumerator).
- **`*ToBytesAsync` restored** (4 methods, `IBufferOwner<byte>`) on top of `StreamExtensions.ReadBytesAsync`
 : the pooled counterpart to the interface's `Task<byte[]>`; the previous session had migrated these to
  `IBufferOwner<byte>` but they only existed inside the commented-out block.
- **New types** `Compression/ArchiveEntry.cs`, `Compression/ArchiveInfo.cs`: nothing implemented
  `IArchiveEntry`/`IArchiveInfo` before.
- Ownership / rewind contract made explicit: DECISIONS #002.

## Files touched
- `Compression/CompressionManager.cs` (full implementation, legacy commented block dropped)
- `Compression/ArchiveEntry.cs`, `Compression/ArchiveInfo.cs` (new)
- `../Tests/Common.Tests/Compression/CompressionManagerTests.cs` (new, 117 tests)

## Verification
`Common.Tests` → **268 passed / 0 failed** (151 pre-existing + 117 new). `ActDim.Practix.Common` builds with
0 errors; the only warnings are pre-existing ones in other files.

## Follow-up in the same session: entry metadata (task P9)
`IArchiveEntry` carried only `FullName` / `Size` / `ArchiveInfo`, which made extract-to-disk impossible: a
directory entry was indistinguishable from an empty file. Added `ArchiveEntryType EntryType` (new enum in
Abstractions), `DateTimeOffset? LastWriteTime`, `long? CompressedSize`, `string LinkTarget`; populated for both
formats via two new mappers (`CreateEntry(ZipArchiveEntry…)` / `CreateEntry(TarEntry…)`) that also replaced four
copies of the same object initializer. ZIP directories are recognized by the empty file-name part (the BCL's own
rule), TAR maps from `TarEntryType`. TAR reports `CompressedSize == null` (entries are stored verbatim); ZIP
timestamps are timezone-less DOS wall clock, documented on the interface after a test caught the offset
difference. Also fixed a format inconsistency in the writer callback path: renaming `entry.FullName` was ignored
by ZIP but honoured by TAR: the name is now captured before the callback in both (test added).
Write side stays open: `ArchiveEntrySource` has no type/timestamp/link, so directories and links can be read but
not produced: folded into task P7. → 276 tests passing.

## Known gaps / follow-ups
- `ICompressionManager` warts worth revisiting (left as declared, not changed):
  `Task<byte[]> DecompressAsync(Stream, …)` breaks the zero-alloc theme and is asymmetric with its
  `ReadOnlyMemory` sibling that returns `Task<Stream>`; `GetArchiveFormat`/`GetCompressionFormat` return a
  non-nullable enum with no `Unknown` member, so "unrecognized" can only be an
  `InvalidDataException` (same for `GetArchiveFormatByFileExtension`): a `TryGet…` pair or an `Unknown`
  member would be cleaner. There is no `CompressAsync(Stream input, Stream output, …)` in the interface;
  added as a public overload here since it is the only truly allocation-free compress path.
- `ArchiveEntrySource.OpenReadAsync` is typed `Func<Stream>` (sync despite the name).
- `CreateTempStream` still always memory-backed: the large-payload FileStream variant remains a TODO.
- `ActDim.Practix.Common` declares its own `InvalidDataException` (empty, no message ctor); the short name
  resolves to it inside `ActDim.Practix.Common.*` namespaces. `System.IO.InvalidDataException` is spelled
  out in both the manager and the tests to avoid that trap.
