# Context

_Current-state snapshot. Keep SHORT; history goes to SESSIONS/._

- `Pooling/AsyncObjectPool.cs`: Bounded, FIFO-ordered async pool coordinated via `SemaphoreSlim` and `ConcurrentQueue<T>`. Supports `PooledObject.DiscardAsync()` / `pool.DiscardAsync(item)` to discard corrupted instances, decrements `_createdCount`, disposes via `_disposer`, and unblocks waiting callers without slot starvation or pool poisoning. `DisposeAsync()` performs fault-tolerant draining, collecting exceptions and throwing `AggregateException`.
- `Compression/CompressionManager.cs` fully implements `ICompressionManager` on the .NET 10 BCL only: GZip / Deflate / Brotli / ZIP / TAR.
- `Extensions/StreamExtensions.cs`: zero-alloc helpers (`GetString`, `WriteString`, `ZeroAllocCopyTo(Async)`, `ToMemory(Async)`, `ReadBytes(Async)` -> `IBufferOwner<byte>`, `WriteInChunks(Async)`).
- `Extensions/StringExtensions.cs`: `ToMemory`/`ToMemoryAsync` encode straight into a pre-sized recyclable stream via `WriteString`.
- `ShortId`: stateless static (`ShortId.Generate(len[, charSet])`) over `RandomNumberGenerator.GetString`.
- `Disposal/DisposableAction`: atomic run-once via `Interlocked.Exchange`. `DisposableBlock<T>` renamed to `DisposableAction<T>`. Async siblings `DisposableAsyncAction` and `DisposableAsyncAction<T>`.
- Tests: `Tests/Common.Tests` -> **246 passing** (`ActDim.Practix.sln` 576 total passing, 677 solution-wide with Three). `ActDim.Practix.Common` builds clean with 0 errors.
