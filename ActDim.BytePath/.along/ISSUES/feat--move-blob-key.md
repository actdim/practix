---
protocol: along
slug: move-blob-key
type: feat
status: open
priority: medium
created: 2026-08-27
updated: 2026-08-27
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# move-blob-key

## Goal

Add `MoveAsync` operation to `IBlobManager` and `BlobManager` in `ActDim.BytePath`, allowing callers to atomically move/rename a blob record and its underlying physical stored content from `sourceKey` to `targetKey`.

## Why `MoveAsync` over `Rename`

In a blob/block storage system, changing a key isn't just metadata relabeling: it requires physically moving or transferring the underlying payload bytes (especially when key prefixes route to different storage backends or subdirectories). Calling the operation `MoveAsync` accurately conveys that both metadata and physical data content are relocated.

## Core Requirements & Design Decisions

### 1. API Surface (`IBlobManager`)
```csharp
Task<BlobResult> MoveAsync(string sourceKey, string targetKey, bool overwrite = false, CancellationToken ct = default);
Task<BlobResult> MoveAsync(string sourceKey, string targetKey, TimeSpan timeout, bool overwrite = false, CancellationToken ct = default);
```

### 2. Concurrency & Deadlock Prevention
- `MoveAsync` requires exclusive `Write` locks on both `sourceKey` and `targetKey`.
- To prevent deadlocks from concurrent inverted calls (e.g. `MoveAsync("A", "B")` vs `MoveAsync("B", "A")`), locks on both keys must be acquired in a deterministic order (e.g. lexicographical sorting of key names before locking).

### 3. Physical Storage Transfer (`IBlobDataStore`)
- **Same-store move:** If `sourceKey` and `targetKey` route to the same `IBlobDataStore`, perform fast physical relocation (e.g. `File.Move` in `FileSystemBlobDataStore`) when possible.
- **Cross-store move:** If keys belong to different `IBlobDataStore` backends (e.g. `fs:` to `s3:`), stream content from source store to target store via `ReadAsync` -> `PutAsync`, then delete physical content at `sourceKey`.

### 4. Conflict Handling (`overwrite` parameter)
- If `targetKey` already exists and `overwrite == false`, fail with a conflict error (`BlobErrorCode.Conflict`) and leave both keys intact.
- If `overwrite == true`, replace existing content and metadata at `targetKey`.

### 5. Registry Transaction
- Update `IBlobRegistry` record from `sourceKey` to `targetKey` within transaction scope, releasing source locks and binding target locks appropriately.

## Acceptance Criteria
- Moving a non-existent `sourceKey` returns `BlobErrorCode.KeyNotFound`.
- Moving to an existing `targetKey` with `overwrite = false` returns `BlobErrorCode.Conflict` without modifying source or target.
- Moving within the same data store physically relocates the stored content and updates registry metadata.
- Cross-store move transfers data between stores and removes the source physical blob.
- Concurrent move operations do not deadlock.

