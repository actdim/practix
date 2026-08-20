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
  `ToMemory(Async)`, `ReadBytes(Async)` → `IBufferOwner<byte>`, `WriteInChunks(Async)`). Note
  `ZeroAllocCopyToAsync` buffers a NON-seekable source entirely into memory — use `Stream.CopyToAsync`
  for decoder output. `WriteInChunks` (was `WriteSafe`) bounds the per-call buffer and, in the async
  form, sets the cancellation granularity; it is pointless for a plain `FileStream`/`MemoryStream`.
- `Extensions/StringExtensions.cs`: `ToMemory`/`ToMemoryAsync` encode straight into a pre-sized
  recyclable stream via `WriteString` — avoid staging through an intermediate buffer. Both return the stream **rewound**.
- Tests: `Tests/Common.Tests` → **238 passing** (`ActDim.Practix.sln` 559 total passing). `ActDim.Practix.Common` builds clean with 0 errors.
- Trap: `ActDim.Practix.Common.InvalidDataException` (empty class) shadows `System.IO.InvalidDataException`
  inside `ActDim.Practix.Common.*` namespaces — always qualify.
- `ShortId` is now a stateless static (`ShortId.Generate(len[, charSet])`) over
  `RandomNumberGenerator.GetString` — cryptographically strong, no instance/disposal. Seed / custom-RNG
  ctors were removed.
- `Messaging/CallContext(+Provider)` is a standalone ambient property bag with scoped push/pop (a
  Serilog-free `LogContext.PushProperty`), backed by `AsyncLocal<ImmutableDictionary<string,object>>`;
  `Set` assigns `.Value` (per-flow isolation), dispose restores from current state. Do not rely on
  `Activity.Current` for corr-id in library contexts — own it here, harmonise via `Activity.Current?.TraceId`.
- `Disposal/DisposableAction`: atomic run-once via `Interlocked.Exchange`. `DisposableBlock<T>` renamed to
  `DisposableAction<T>`; its carried state is now private (alloc-free delegate+state, released on dispose).
  Async siblings `DisposableAsyncAction` and `DisposableAsyncAction<T>` (`IAsyncDisposable`) added.
- A 2026-08-05 review flagged many still-open issues across the assembly (Introspection SO on
  self-referential types, Json converter bugs, MathExtensions/Task* BCL duplication, etc.) — see
  SESSIONS/2026/2026-08-05--common-review-fixes.md.
- Watch: `ActDim.Practix.DataAccess` has unrelated pre-existing build errors (`OrthoBits.Abstractions`).
