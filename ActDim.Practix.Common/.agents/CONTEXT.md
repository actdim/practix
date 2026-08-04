# Context

_Current-state snapshot. Keep SHORT; history goes to SESSIONS/._

- `Compression/CompressionManager.cs` fully implements `ICompressionManager` on the .NET 10 BCL only: GZip /
  Deflate (raw) / Brotli codecs, ZIP via the new async `ZipArchive` API, TAR via `System.Formats.Tar` (PAX).
  BZip2/LZMA/LZMA2/PPMd/7z/RAR are detected by signature but throw `NotSupportedException` (no BCL codec).
- Contract: streams this class creates come back rewound and caller-owned; a caller-supplied destination is
  never rewound nor closed; entry streams passed to reader/writer callbacks are disposed when the callback
  returns and may be opened once per entry.
- Extras beyond the interface: `CompressAsync(input, output, …)` and 4 `*ToBytesAsync` returning
  `IBufferOwner<byte>` (pooled; caller disposes). Tunables are `protected virtual`
  (`DefaultCompressionLevel`, `DefaultArchiveFormat`, `BufferSize`, `CreateTempStream`).
- `Compression/ArchiveEntry.cs` + `ArchiveInfo.cs` are the only `IArchiveEntry`/`IArchiveInfo` implementations.
- `IArchiveEntry` now carries `EntryType` (new `ArchiveEntryType` enum), `LastWriteTime`, `CompressedSize`,
  `LinkTarget` — filled for both formats, so extract-to-disk is implementable. READ ONLY: `ArchiveEntrySource`
  still has no type/timestamp/link, so directories and links can be read but not written (task P7).
  Format quirks: TAR reports no per-entry `CompressedSize`; ZIP timestamps are timezone-less DOS wall clock.
- `Extensions/StreamExtensions.cs`: zero-alloc helpers (`GetString`, `WriteString`, `ZeroAllocCopyTo(Async)`,
  `ToMemory(Async)`, `ReadBytes(Async)` → `IBufferOwner<byte>`). Note `ZeroAllocCopyToAsync` buffers a
  NON-seekable source entirely into memory — use `Stream.CopyToAsync` for decoder output.
- Tests: `Tests/Common.Tests` → **276 passing** (`Compression/CompressionManagerTests.cs` 125,
  `Extensions/StreamExtensionsTests.cs` ~44). `ActDim.Practix.Common` builds clean.
- Trap: `ActDim.Practix.Common.InvalidDataException` (empty class) shadows `System.IO.InvalidDataException`
  inside `ActDim.Practix.Common.*` namespaces — always qualify.
- Watch: `ActDim.Practix.DataAccess` has unrelated pre-existing build errors (`OrthoBits.Abstractions`).
