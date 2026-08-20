# Issues   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active
- [ ] feat--ambient-context-register-for-dispose — Decoupled `RegisterForDispose` in AmbientContext for Web requests & background scopes.
- [ ] feat--memory-arena-auto-cleanup — Memory Arena pattern with automatic cleanup of rented streams/buffers upon scope disposal.
- [ ] feat--iconfiguration-application-config-manager — Convenient application configuration manager based on IConfiguration.
- [~] debt--compression-interface-cleanup — 14 documented problems in `ICompressionManager` / `IArchiveEntry` / `ArchiveEntrySource`.
- [ ] task--compression-large-payload-spill — `CompressionManager.CreateTempStream` memory vs FileStream spilling.
- [ ] task--adaptive-stream — read-only seekable `Stream` for memory/temp-file spilling.
- [ ] feat--dynamic-array-json-converter — Evaluate DynamicArray wrapper for JSON array deserialization in ObjectJsonConverter.
- [ ] debt--stringsplit-regex-cache — Cache compiled Regex in StringExtensions.Split.
- [ ] debt--arraysegment-blockcopy-optimization — Evaluate Buffer.BlockCopy / MemoryMarshal fast path in ArraySegmentExtensions.CloneToArray.
- [ ] feat--large-payload-compression — Implement streaming/spill-to-file compression for large payloads in CompressionManager.
- [ ] feat--encoding-async-extensions — Async Encoding stream extensions (GetStringAsync and CopyToStreamAsync).
- [ ] feat--enumerable-estimation-extensions — Enumerable estimation and predicate extensions.

## Done (recent)
- [x] debt--ambient-context-direct-storage (2026-08-19) — Direct AsyncLocal in AmbientContext, removed AmbientContextProvider, added scoped extensions and delegates.
- [x] debt--enumerable-dead-code (2026-08-19) — Removed dead/commented code from EnumerableExtensions.cs and modernized Chunk partitioning.
- [x] debt--factorydict-replace-rwlock (2026-08-19) — Replaced ReaderWriterLockSlim in FactoryDictionary / FuncExtensions with lock-free ConcurrentFactoryDictionary.
- [x] debt--remove-autofac-dependency (2026-08-17) — Remove Autofac dependency and migrate to standard Microsoft Dependency Injection.
- [x] feat--extract-practix-json-assembly (2026-08-17) — Extract JSON serialization subsystem into dedicated ActDim.Practix.Json assembly.
- [x] debt--json-serializer-reflectron-optimization (2026-08-17) — Replace un-cached reflection in StandardJsonSerializer with fast compiled expression tree setters and property metadata cache.
