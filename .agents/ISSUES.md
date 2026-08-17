# Active Issues

_Solution-level issues board. Project-specific issues live in their respective subproject folders (e.g. `ActDim.Observability/.agents/`, `ActDim.BlobManager/.agents/`, etc.)._

## Active

## Backlog
- `debt--enumerable-dead-code`: Remove dead/obsolete code from EnumerableExtensions
- `debt--factorydict-replace-rwlock`: Replace ReaderWriterLockSlim in FactoryDictionary with ConcurrentDictionary
- `debt--stringsplit-regex-cache`: Cache compiled regex in StringExtensions.Split
- `debt--arraysegment-blockcopy-optimization`: Evaluate Buffer.BlockCopy / MemoryMarshal fast path in ArraySegmentExtensions.CloneToArray
- `feat--large-payload-compression`: Implement streaming/spill-to-file compression for large payloads in CompressionManager
- `feat--encoding-async-extensions`: Async Encoding stream extensions (GetStringAsync and CopyToStreamAsync)
- `feat--enumerable-estimation-extensions`: Enumerable estimation and predicate extensions

## Done (recent)
- `feat--emitron-tests`: Create ActDim.Emitron unit test coverage for Roslyn compilation and evaluation
