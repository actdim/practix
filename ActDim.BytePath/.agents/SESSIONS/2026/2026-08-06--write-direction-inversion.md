---
date: 2026-08-06
slug: write-direction-inversion
agent: Claude Code / claude-opus-5[1m]
branch: main
commit: 4995d1e
summary: >
  Inverted the data-store write direction so the store consumes a stream instead of handing one out,
  added a producer-delegate overload as a default interface method with a direct file-system override,
  wrote the consumer-facing README, and recorded the object-store constraints that shaped all of it.
---

# Write direction inverted; portability constraints written down

Continues `blob-content-lifecycle` (2026-08-05). That session fixed the registry/data-store
reconciliation; this one questioned the shape of the write API itself and changed it.

## Why

Two threads drove everything:

**Portability.** The whole point of the `IBlobRegistry` / `IBlobDataStore` split is that either side
can be replaced, so every contract decision has to stay expressible on an object store. Walking
through what S3 and Azure Blob actually offer turned several of yesterday's decisions from "we chose
this" into "it cannot be otherwise": and exposed one where our shape was simply wrong.

**The returned write stream.** `WriteAsync` handed out a stream for the caller to write into. That was
the root of the disposal-order convention (#003) and the dispose-time size refresh (#002). On an
object store it is worse than a wrong `Size`: a multipart upload does not exist until
`CompleteMultipartUpload`, so a mis-ordered dispose means a missing object with the lock already
released: and reconciliation (#001) then treats the record as orphaned and deletes it.

## What changed

### Write direction inverted (#006)

```csharp
Task<long> WriteAsync(BlobRecord record, Stream content, CancellationToken ct);
Task<long> AppendAsync(BlobRecord record, Stream content, CancellationToken ct);
```

Both return the resulting total size. The write is finished when the call returns, so there is no
stream left alive, no disposal order to get wrong, and the size is known exactly once: at the moment
the bytes land. Reading still hands a stream out; the asymmetry mirrors every storage SDK.

`TrackSizeOnDispose` (#002) **stays** despite the inversion: `BlobRecord.Size` has an `internal`
setter, so a data store implemented in another assembly cannot record the size itself.

### Producer-delegate overload, and where it lives

A producer whose API only writes (`JsonSerializer.SerializeAsync`, `XmlWriter`, `GZipStream` compress)
has no readable form to hand over, so `IBlobDataStore` also takes the producer:

```csharp
Task<long> WriteAsync(BlobRecord record, Func<Stream, CancellationToken, Task> produce, CancellationToken ct);
```

Completion stays out of the caller's hands: returning from the delegate *is* the signal. The guarantee
is structural rather than type-level: the same way a `using` block works.

This was first built as extension methods, which was a mistake corrected within the session: an
extension is static, so a backend could not specialise it, and worse, a call through `IBlobDataStore`
would silently pick the extension over a same-named instance method. It is now a **default interface
method**. `ProducerStreamBridge` (internal) holds the pipe-based default;
`FileSystemBlobDataStore` overrides both and hands its own `FileStream` over, since bridging buys
nothing where the store already owns a writable destination.

Two consequences: the supplied stream's seekability now varies by store, so the contract promises only
that it is write-only; and `WriteThroughAsync` reports `file.Length` rather than `file.Position`,
because a producer holding the real file stream may seek within it.

`OpenWriteAsync` was considered and rejected: it hands completion back to the caller and reinstates
everything #006 removed, and reintroduces the "which write method?" fork #005 killed.

### Zero lock timeout means "do not wait"

Bulk deletion expressed "skip anything locked" as a 1 ms timeout, because the acquisition loop treated
`timeout <= TimeSpan.Zero` as "unspecified" and substituted the default. That also only worked
probabilistically: an attempt finishing under a millisecond sent the loop through another 100 ms
delay. Now only a negative value means unspecified, and `TimeSpan.Zero` attempts once and gives up by
construction. `NormalizeAcquireTimeout` in both registries; the lock's own 1 s TTL floor is untouched.

### Read streams are promised seekable

`range-read`'s first half. `ReadAsync` already returned a seekable `FileStream`; the contract said
nothing, which is the worst state, since callers would depend on discovered behaviour and break on
S3's forward-only `GetObject`. Now documented and pinned by a test. The explicit range overload stayed
deferred: a seek plus a sized read already maps to one range GET, and for resumable download seeking
is the optimal pattern, not merely an adequate one.

### Documentation

- **`README.md`**: new, consumer-facing: the API plus the reasoning behind the shapes that look
  unusual. The write-direction section is the centre of it.
- **`AGENTS.md`**: the write-surface section rewritten twice as the design moved.
- **`VISION.md`**: was an empty skeleton. Now carries the object-store capability table (what S3 and
  Azure can and cannot do per operation, with why each of our decisions follows), the non-goals as
  deliberate refusals rather than gaps, and a roadmap ordered by what unblocks a non-file-system
  backend. Corrected mid-session: pull-vs-push is an AWS-SDK trait, not a property of object stores -
  Azure ships `OpenWriteAsync` and a seekable `OpenReadAsync`.
- **Braces**: swept the project and tests for brace-less `if`/loops per the repo style; seven found,
  all in pre-existing code. `using` declarations left alone: they have no body to brace.

### Sibling repo

`CanarySystems.FileStorage`'s `SQLiteBlobRegistry` received yesterday's deletion changes in its own
query style (`database.Sql.Table<>().Params().Generate()`, `CommonDatabaseOperation`, explicit
transactions, `NOT IN`), plus today's timeout normalization: which matters more there, since its
`_defaultTimeout` is hard-wired to 5 minutes, so bulk cleanup would have stalled 5 minutes per locked
key.

## Files touched

- `IBlobDataStore.cs`: inverted writes, producer-delegate default methods, seekable-read promise
- `FileSystemBlobDataStore.cs`: `WriteThroughAsync` behind both content and producer forms; override
- `ProducerStreamBridge.cs`: new (replaced `BlobDataStoreExtensions.cs`, deleted)
- `SQLiteBlobRegistry.cs`: `NormalizeAcquireTimeout`; brace fixes
- `BlobManager.cs`: `SkipLockedTimeout` removed in favour of `TimeSpan.Zero`
- `README.md`: new
- `AGENTS.md`, `.agents/VISION.md`, `.agents/DECISIONS.md`, `.agents/GLOSSARY.md`
- `Tests/BlobManager.Tests/BlobManagerTests.cs`: 50 tests (was 30 at yesterday's start, 8 of them red)

## Decisions

#006 (new) · #002, #003, #005 amended where this session changed what they said · #004 amended with
the zero-timeout contract

## Tasks

- `range-read`: first half done (seekable promise), second half deferred with the reasoning recorded
- `batch-content-delete`: new, open

## Gaps / follow-ups

- **DI registration is the next real blocker.** `BlobManager` is still `internal`;
  `BlobManagerModule.cs` exists but is entirely commented out, so nothing outside the assembly can
  consume the library. README says so explicitly.
- `Hash` is still never computed from content: but the inversion created the place to do it, since
  the bytes now flow through `WriteThroughAsync`. Worth a task.
- A producer that fails mid-write leaves partial content (the file was already truncated). Reconcile
  then reports the partial length as the size, so a truncated blob looks valid. Predates this session
  and applies to the plain `Stream` overload too.
- Nullable reference types still disabled; no `IAsyncEnumerable` variant of `QueryAsync`.
