# Vision

_North star: scope, boundaries, non-goals, roadmap. Evolves slowly; slims as features ships._

## Scope

Keyed blob storage with metadata, expiration and distributed read/write locking, split into two
layers that know nothing about each other — `IBlobRegistry` (metadata + locks) and `IBlobDataStore`
(content). `BlobManager` is the only place that spans them.

The split exists so both sides can be replaced: SQLite → PostgreSQL/Redis for the registry, file
system → S3/Azure Blob/GCS for the content. **Every contract decision is measured against that**, so
the surface must stay expressible on an object store, not just on a file system.

## What object stores can and cannot do

This is the constraint that shapes the content contract. Established while designing decisions
#002 and #005 — check it before adding anything to `IBlobDataStore`.

| operation | S3 | Azure Blob | notes |
|---|---|---|---|
| whole-object write | `PutObject` ≤ 5 GiB, or multipart (parts ≥ 5 MiB, ≤ 10 000, object ≤ 5 TiB) | `PutBlob` / staged blocks | both SDKs *consume* a stream (`PutObjectRequest.InputStream`, `BlobClient.UploadAsync`), which is why #006 inverted our write direction to match — no adapter needed |
| append | **none** — objects are immutable; emulated by re-upload or `UploadPartCopy` (also ≥ 5 MiB per part) | `AppendBlob` + `AppendBlock`, 4 MiB per block | why `AppendAsync` is its own method: a backend refuses it by signature, not by inspecting a parameter |
| write at a position | **none** anywhere. No `SetLength`, no partial overwrite | — | why positioned writing was dropped in #005 |
| read at a position | `Range: bytes=a-b` — first-class | same, and `OpenReadAsync` returns a **seekable** stream backed by range GETs | portable, unlike positioned writing |
| size / existence | `HeadObject` — one request answers both | `GetProperties` | why `GetSizeAsync` is the single primitive and `ExistsAsync` derives from it (#002) |
| delete | `DeleteObject`; batch `DeleteObjects` up to 1000 keys | batch via `SubmitBatch` | our bulk deletion currently issues one call per key |
| locking | none (Object Lock is WORM retention, not mutual exclusion) | none | why locks live in the registry, never in the data store |
| stable location | presigned URL only, and it **expires** | SAS URL, expires | `ResolveLocationAsync` returns a permanent path today; the meaning diverges on object stores |

Two consequences that will bite an object-store backend and do not show up on a file system:

- **Durability lands on stream dispose.** A multipart upload does not exist until
  `CompleteMultipartUpload`, and Azure's `OpenWriteAsync` commits its blocks on flush/dispose the
  same way — this is inherent to the model, not an artefact of one SDK. Disposing the record before
  the stream costs a wrong `Size` on disk (#003); on an object store it costs a **missing object**
  with the lock already released. The ordering convention is the same, the price is higher.
- **An aborted write leaks billable parts.** Incomplete multipart uploads are charged until aborted
  or swept by a lifecycle rule. Neither the registry nor `CleanupAsync` knows about them — a third
  state that does not exist on a file system.

## Non-goals

- Public positioned writing at an arbitrary offset. Backends differ too much; resumable out-of-order
  uploads belong to a durable multipart upload session, which owns staging and completion.
- Locking inside the data store. Mutual exclusion is the registry's job and stays portable that way.
- Computing content hashes implicitly on every write — that means reading the whole blob.
- Enforcing stream/record disposal order by making `BlobRecord` own its streams (#003). The record
  stays a lightweight near-POCO.

## Roadmap

Ordered by how much each unblocks a non-file-system backend.

1. **DI registration helper.** `BlobManager` is `internal` with no factory; tests construct it
   directly. Blocks all real consumption.
2. **`range-read`** — state that read streams are seekable. They already are on a file system, and
   the contract's silence is what will break callers on a forward-only backend. The explicit range
   overload is deferred: a seek plus a sized read already maps to one range GET, and for resumable
   download — seek, then read to the end — seeking is the optimal pattern, not merely an adequate one.
3. **`batch-content-delete`** — bulk deletion currently deletes content one key at a time.
4. **Alternative `IBlobDataStore`** — S3 or Azure, which is what validates the contract above.
5. **Alternative `IBlobRegistry`** — PostgreSQL or Redis. `SQLiteBlobRegistry` already proves the
   contract is implementable over a different query layer (see the sibling copy in
   `CanarySystems.FileStorage`).
6. Content hashing as an explicit opt-in; tag-based query; `IAsyncEnumerable` variant of
   `QueryAsync`; nullable reference types.
7. Multipart upload sessions for resumable out-of-order uploads, after the session persistence and
   final-publication rules are designed.
