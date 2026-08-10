# add-try-create-with-conflict-behavior

## Goal
Add `TryCreateAsync` method that always creates a new blob record. If the key already exists, behavior depends on `ConflictBehavior`.

Also add a convenience creation method that combines lock acquisition + content write in one call —
so the caller does not have to go through `TryGetOrSetAsync` → `WriteAsync` when they already have
the content (byte[], Stream, or producer delegate).

## Decisions

### Conflict behavior (core)

- **New enum**: `BlobErrorCode.Conflict` — "key already exists" (not KeyNotFound)
- **New enum**: `ConflictBehavior` with values `Fail` and `Replace`
- **Options + Behavior are mandatory** — no overload without them
- Two overloads: default timeout, explicit TimeSpan timeout

### Convenience creation (one-shot blob with content)

- **Form**: extension methods on `IBlobManager` (not a new interface member)
  - Rationale: `IBlobManager` is the only layer that sees both registry and data store;
    an extension lets the caller write `manager.CreateAsync(key, content, options, ct)` without
    cluttering the interface. A concrete class method would be redundant (tests use internals
    directly), and a `BlobManager`-only method would not reach through the interface.
  - Alternative considered: adding to `IBlobManager` directly. Rejected — the creation
    ergonomics are caller-facing, not a store capability. Extensions keep the interface stable
    for backends.
- **Overloads** (mirroring the write surface of `IBlobDataStore`):
  - `CreateAsync(key, ReadOnlyMemory<byte>, BlobStoreOptions, ConflictBehavior, timeout?, ct)`
  - `CreateAsync(key, Stream, BlobStoreOptions, ConflictBehavior, timeout?, ct)`
  - `CreateAsync(key, Func<Stream, CancellationToken, Task>, BlobStoreOptions, ConflictBehavior, timeout?, ct)` — producer delegate
  - Plus non-options variants (defaults: `LockType.Write`, `ConflictBehavior.Fail`)
- **Implementation**: internally calls `TryCreateAsync` with the same `ConflictBehavior`, then
  writes the content through `DataStore.WriteAsync` (or `AppendAsync` for streams that can't be
  rewound). The lock is held until the record is disposed, so disposal order stays correct.
- **Return**: `(BlobErrorCode, BlobRecord)` — same as `TryCreateAsync`. `IsNew = true` when
  successful (always creates new).
- **Not a `BlobManager` method**: keeping it as an extension means a future DI registration helper
  (task `di-registration`) can expose it without adding interface members.

## Implementation steps

1. Add `BlobErrorCode.Conflict` to existing enum
2. Create `ConflictBehavior.cs` (Fail / Replace)
3. Add internal `TryCreateAsync` to `IBlobRegistry`
4. Implement in `SQLiteBlobRegistry`:
   - `Fail`: INSERT OR IGNORE → rows == 0 → `(Conflict, null)`
   - `Replace`: delete old record + content → create new → write-lock → apply options → return with `IsNew = true`
5. Add `TryCreateAsync` public overloads to `IBlobManager`
6. Implement `TryCreateAsync` in `BlobManager` — reconcile content with `allowNew: true`, TrackSizeOnDispose
7. Create `BlobManagerExtensions.cs`:
   - `CreateAsync` overloads for `byte[]`, `Stream`, producer delegate
   - Each calls `TryCreateAsync` internally, then writes content, returns `(errorCode, record)`
8. Update README.md
9. Write tests

## Files touched
- `BlobRecord.cs` (no changes)
- `BlobErrorCode.cs` — new Conflict value
- `ConflictBehavior.cs` — new file
- `IBlobRegistry.cs` — internal TryCreateAsync
- `SQLiteBlobRegistry.cs` — implementation
- `IBlobManager.cs` — public TryCreateAsync overloads
- `BlobManager.cs` — orchestration + reconcile
- `BlobManagerExtensions.cs` — **new file**, convenience creation methods
- `README.md` — docs
- Tests (BlobManagerTests.cs)
