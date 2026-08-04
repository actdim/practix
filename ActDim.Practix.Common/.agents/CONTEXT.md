# Context

_Current-state snapshot. Keep SHORT; history goes to SESSIONS/._

- `Extensions/StreamExtensions.cs` reworked for correctness + zero-alloc: `ReadExactly` for full reads; pooled fallbacks (no broken `GetBuffer()`); shared `PooledCopy`/`PooledCopyAsync` copy loop; `WriteString` encodes via pooled `GetBytes` (zero-alloc, no BOM); `ReadBytes`/`ReadBytesAsync` return `IBufferOwner<byte>`.
- Read-to-string API renamed `ToString`/`ToStringAsync` → `GetString`/`GetStringAsync` (avoids `object.ToString()` collision). Callers updated: `EncodingExtensions`, `EntityFetcher`.
- `Compression/CompressionManager.*ToBytesAsync` (8 methods) now return `IBufferOwner<byte>` — caller must dispose.
- Tests: `Tests/Common.Tests/Extensions/StreamExtensionsTests.cs` (~44). `Common.Tests` → 151 passing; `ActDim.Practix.Common` builds clean.
- Watch: `ActDim.Practix.DataAccess` has unrelated pre-existing build errors (`OrthoBits.Abstractions`).
