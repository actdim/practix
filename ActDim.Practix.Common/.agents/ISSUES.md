# Issues   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active
- [~] debt--compression-interface-cleanup — 14 documented problems in `ICompressionManager` / `IArchiveEntry` / `ArchiveEntrySource`.
- [ ] task--compression-large-payload-spill — `CompressionManager.CreateTempStream` memory vs FileStream spilling.
- [ ] task--adaptive-stream — read-only seekable `Stream` for memory/temp-file spilling.
- [ ] feat--dynamic-array-json-converter — Evaluate DynamicArray wrapper for JSON array deserialization in ObjectJsonConverter.
- [ ] debt--enumerable-dead-code — Remove dead/obsolete commented code from EnumerableExtensions.cs.
- [ ] debt--factorydict-replace-rwlock — Replace ReaderWriterLockSlim in FactoryDictionary with ConcurrentDictionary.
- [ ] debt--stringsplit-regex-cache — Cache compiled Regex in StringExtensions.Split.
- [ ] debt--arraysegment-blockcopy-optimization — Evaluate Buffer.BlockCopy / MemoryMarshal fast path in ArraySegmentExtensions.CloneToArray.
- [ ] feat--large-payload-compression — Implement streaming/spill-to-file compression for large payloads in CompressionManager.
- [ ] feat--encoding-async-extensions — Async Encoding stream extensions (GetStringAsync and CopyToStreamAsync).
- [ ] feat--enumerable-estimation-extensions — Enumerable estimation and predicate extensions.

## Done (recent)
- [x] compression-manager-net10 (2026-08-04) — full `ICompressionManager` implementation on .NET 10 BCL.
- [x] stream-extensions-hardening (2026-08-04) — correctness + zero-alloc rework.
