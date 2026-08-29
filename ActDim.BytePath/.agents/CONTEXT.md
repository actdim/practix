# Context

_Current-state snapshot. Keep SHORT; history goes to SESSIONS/._

- **State**: `IBlobDataStore.PutAsync` is the verified whole-blob create-or-replace operation
  (101/101 BlobManager tests green). Multipart upload sessions are planned, not implemented.
- **Shape**: two layers that do not know each other: `SQLiteBlobRegistry` (metadata + locks) and
  `FileSystemBlobDataStore` (content). `BlobManager` is the only place that sees both, so
  `ReconcileContentAsync` and all four deletion paths live there.
- **Read `DECISIONS.md` #001–#011 before changing any invariant.** In short: a record without content
  is transient and only `TryGetOrSetAsync` may observe it, via `IsNew`; `Size` is library-owned and
  always read from the store; `BlobRecord` stays decoupled from streams; deletion removes content
  before metadata; the write surface has no position and no mode flags; **writes consume a stream,
  reads hand one out**; options are instructions and the record is state, so `BlobRecord.Apply`
  translates one into the other under a write lock: and `TryGetForWritingAsync(key, options, …)` is
  that acquire-then-`Apply` pair as one call, persisting on dispose (#008).
- **Docs discipline**: XML docs say what a member does for the caller. Rationale goes to
  `DECISIONS.md`, requirements on implementations to `AGENTS.md`, and `README.md` is the only place
  that deliberately explains why the API is shaped as it is.
- **Before touching `IBlobDataStore`**: read the object-store capability table in `VISION.md`. The
  contract is deliberately kept expressible on S3/Azure, not just on a file system.
- **Traps**: lock acquisition is not re-entrant: hence no self-locking delete in the registry.
  `TimeSpan.Zero` as a lock timeout means "try once"; only a negative value means "unspecified".
  Read streams are promised seekable; the producer-delegate stream is **not** promised seekable.
- **Docs**: `README.md` is consumer-facing (API + why the write direction is inverted); `AGENTS.md` is
  the invariant list for agents. Keep both in sync with the public surface.
- **Open tasks**: `content-hash`, `integrity-audit`, `read-lock-persists-mutations`,
  `batch-content-delete`, `range-read`'s deferred second half, `add-try-create-with-conflict-behavior`
  (one-shot `CreateAsync` extension methods), and `multipart-upload-session` for resumable out-of-order uploads.
- **`IBlobDataStore` cannot enumerate its content**, so nothing can find orphaned files or audit the
  store against the registry: see `integrity-audit`.
- **Key format**: `:` (colon) is the default URL-safe logical hierarchy separator per RFC 3986 `pchar` (#011).
  `FileSystemBlobDataStoreOptions.HierarchySeparator` controls on-disk directory splitting (defaults to `':'`, `null` for uniform hash-sharding).
  `EscapeFileName` uses reversible `%XX` escaping and protects Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
- **Sibling copy**: `CanarySystems.FileStorage` carries the same design over a different query layer
  and has received these changes. Its `BlobManager` ctor takes a logger provider, its data store has a
  flat path layout and an `IFileStorageConfiguration` ctor, and its registry timeout is hard-wired to
  5 minutes with no overload: preserve those when syncing.
