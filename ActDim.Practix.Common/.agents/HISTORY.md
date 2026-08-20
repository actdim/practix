# History

_Index of sessions (newest last). One line per session:_
_`<YYYY-MM-DD> — <slug> — <agent> — <summary> — <relative link>`_

- 2026-08-04 — stream-extensions-hardening — Claude Code / claude-opus-4-8 — Correctness + zero-alloc rework of StreamExtensions, ReadBytes → IBufferOwner, ToString → GetString, ~44 tests (151 passing) — SESSIONS/2026/2026-08-04--stream-extensions-hardening.md
- 2026-08-04 — compression-manager-net10 — Claude Code / claude-opus-5[1m] — CompressionManager implemented on the .NET 10 BCL (async ZipArchive, System.Formats.Tar, GZip/Deflate/Brotli), pooled/zero-alloc paths, ArchiveEntry/ArchiveInfo, 117 tests (268 passing) — SESSIONS/2026/2026-08-04--compression-manager-net10.md
- 2026-08-05 — common-review-fixes — Claude Code / claude-opus-4-8 — Broad critical review of ActDim.Practix.Common + 4 targeted fixes: ShortId → static crypto (RandomNumberGenerator.GetString), EnumerableExtensions.While index, CallContext → AsyncLocal<ImmutableDictionary> (restore + isolation), DisposableAction atomic + DisposableBlock<T>→DisposableAction<T> + async variants — SESSIONS/2026/2026-08-05--common-review-fixes.md
- 2026-08-06 — string-to-memory-fixes — Claude Code / claude-opus-5[1m] — StringExtensions.ToMemory now encodes straight into the stream (one copy instead of two) and both overloads return a rewound stream; WriteSafe→WriteInChunks using the shared BufferSize (276 passing) — SESSIONS/2026/2026-08-06--string-to-memory-fixes.md
- 2026-08-20 — remove-memory-stream-manager-extensions — Antigravity / Gemini 3.6 Flash — Removed dead MemoryStreamManagerExtensions class and GetContextStream methods (559 tests passing) — [2026-08-20--remove-memory-stream-manager-extensions.md](file:///d:/Src/my/actdim/public/dotnet/ActDim.Practix.Common/.agents/SESSIONS/2026/2026-08-20--remove-memory-stream-manager-extensions.md)
