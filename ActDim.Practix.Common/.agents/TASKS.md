# Tasks   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active
- [~] compression-interface-cleanup — 14 documented problems in `ICompressionManager` / `IArchiveEntry` /
  `ArchiveEntrySource`, with options + suggested order. **P9 done** (entry metadata: `EntryType` /
  `LastWriteTime` / `CompressedSize` / `LinkTarget` — extraction is now implementable; the write-side half
  moved into P7). Next: `Task<byte[]>` decompress overload, missing compress-into-caller-stream overload,
  detection API that cannot say "unknown" nor go async. Free to change — zero call sites; 3 decisions pending.
- [ ] compression-large-payload-spill — `CompressionManager.CreateTempStream` is always memory-backed; add the
  FileStream-spilling variant for large payloads (long-standing TODO in the file). Waits on adaptive-stream.
- [ ] adaptive-stream — read-only seekable `Stream` that keeps small payloads in memory and spills large ones
  to a temp file. Spec supplied; 10 open questions first, the big one being whether it also exposes a
  writable-then-sealed mode (without it, it does NOT unblock compression-large-payload-spill).

## Done (recent)
- [x] compression-manager-net10 (2026-08-04) — full `ICompressionManager` implementation on the .NET 10 BCL,
  pooled/zero-alloc paths, ArchiveEntry/ArchiveInfo, 117 tests (268 passing) →
  SESSIONS/2026/2026-08-04--compression-manager-net10.md
- [x] stream-extensions-hardening (2026-08-04) — correctness + zero-alloc rework, ReadBytes → IBufferOwner, ToString → GetString → SESSIONS/2026/2026-08-04--stream-extensions-hardening.md
