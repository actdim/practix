# add-write-at-offset

## Goal
Add `truncate` option to producer-delegate write method and `offset` parameter to stream-based write method, enabling chunked random-write patterns (resumable upload with out-of-order chunks).

## Decisions

- **New overload**: `WriteAsync(record, produce, truncate, ct)` — delegate gets a FileStream; `truncate=false` uses `FileMode.OpenOrCreate` instead of `FileMode.Create`, preventing truncation of existing content. Default bridge (pipe-based) keeps truncate=true semantics.
- **New overload**: `WriteAsync(record, stream, offset, ct)` — opens file with `FileMode.OpenOrCreate`, seeks to `offset`, writes the stream content, returns resulting total size. Offset defaults to 0 for new files.
- Both overloads are on `IBlobDataStore` — default interface throws `InvalidOperationException` (not supported by pipe bridge or non-seekable stores). `FileSystemBlobDataStore` overrides both with real implementation.

## Implementation steps

1. Add overload to `IBlobDataStore`: `WriteAsync(record, produce, bool truncate, CancellationToken ct)`
2. Add overload to `IBlobDataStore`: `WriteAsync(record, Stream content, long offset, CancellationToken ct)`
3. Implement in `FileSystemBlobDataStore`:
   - Producer delegate: `FileMode.OpenOrCreate` when `truncate=false`, `FileMode.Create` when `true`
   - Stream write with offset: `FileMode.OpenOrCreate` + `file.Seek(offset)` before writing
4. Keep default interface methods unchanged (bridge path)
5. Update README.md — document the new overloads and their chunked upload use case
6. Write tests:
   - Producer delegate with truncate=false on existing file → no truncation
   - Stream write with offset → writes at specified position
   - Out-of-order chunks: write chunk 0, then chunk 512, verify both present
   - New file with offset > 0 → file grows to accommodate (sparse or filled)

## Files touched
- `IBlobDataStore.cs` — two new overloads
- `FileSystemBlobDataStore.cs` — override implementations
- `README.md` — docs
- Tests (BlobManagerTests.cs)
