# ActDim.Practix.BlobManager

Keyed blob storage with metadata, expiration and distributed read/write locking.

Two layers that know nothing about each other:

- **`IBlobRegistry`** — metadata and locks. `SQLiteBlobRegistry` ships in the box.
- **`IBlobDataStore`** — the bytes. `FileSystemBlobDataStore` ships in the box.

`BlobManager` is the only component that sees both, so everything spanning the two — verifying that a
record still has content, deleting both halves in the right order — lives there and nowhere else.

The split exists so either side can be swapped: PostgreSQL or Redis for the registry, S3 or Azure Blob
for the content. Every decision in the API below was measured against that, which is the reason for
several shapes that look unusual at first. Where that is the case, this file says why.

> **Status.** `BlobManager` is currently `internal` and there is no DI registration helper yet, so the
> library is consumed from inside the assembly. A registration helper is the next item on the roadmap.

---

## The model

A **record** is the metadata row for a key. It exists independently of the content: creating one
reserves the key and takes a lock, nothing more.

Every acquiring call returns a `BlobResult`, which deconstructs into `(BlobErrorCode, BlobRecord)`:

| `BlobErrorCode` | meaning |
|---|---|
| `None` | success — `Record` is non-null and holds a lock |
| `KeyNotFound` | no record, or its content is gone and you asked for existing content |
| `Timeout` | the lock could not be acquired in time |

A successful result is a **handle**: it holds a read or write lock until disposed. Always
`await using` it — the synchronous `Dispose()` only fires the sync callback.

```csharp
var (ec, record) = await manager.TryGetOrSetAsync("report/2026-08.pdf", ct);
if (ec == BlobErrorCode.None)
{
    await using (record)
    {
        // record holds a write lock here
    }   // lock released, timestamps and size persisted
}
```

Read locks admit concurrent readers and exclude writers. Write locks are exclusive. **Acquisition is
not re-entrant** — asking for a lock you already hold will spin until it times out.

### `IsNew` means "there is no content yet"

Not "the record was just inserted". `TryGetOrSetAsync` sets it both for a key it just created and for
a record whose content has gone missing. Either way it is telling you the same thing: you have to
produce the content.

`TryGetForReadingAsync` and `TryGetForWritingAsync` take the opposite stance — you asked for existing
content, so if there is none the orphaned record is deleted and you get `KeyNotFound`.

### Choosing an entry point

The three differ by more than "creates the record or not". The row that catches people out is the
second one: asking for *existing* content when there is none does not merely fail, it **prunes** the
orphaned record.

| | `TryGetOrSetAsync` | `TryGetForReadingAsync` | `TryGetForWritingAsync` |
|---|---|---|---|
| no record | creates it, `IsNew = true` | `KeyNotFound` | `KeyNotFound` |
| record but no content | succeeds, `IsNew = true` | deletes the record → `KeyNotFound` | deletes the record → `KeyNotFound` |
| lock you get | write — or read, if you ask for it *and* the record already existed | read | write |
| codes it can return | `None`, `Timeout` | `None`, `KeyNotFound`, `Timeout` | `None`, `KeyNotFound`, `Timeout` |
| `IsNew` | meaningful | always `false` | always `false` |
| `BlobStoreOptions` | applied **and written** during the call | no overload — see below | applied, written when you dispose |
| reach for it when | the blob may not exist yet | you only need to read | the blob exists and you will change it |

Notes on the less obvious cells:

- **`TryGetOrSetAsync` never returns `KeyNotFound`, but it is not "always success"** — it still returns
  `Timeout` when the lock cannot be acquired, and again if the read-lock downgrade times out.
- **Options are persisted eagerly only by `TryGetOrSetAsync`**, because it may release the write lock to
  hand you a read lock instead; anything not written before that release would be lost in the gap.
  `TryGetForWritingAsync` holds its write lock until you dispose, and disposal persists the record, so
  nothing extra is written during the call.
- **There is no reading overload taking options.** Applying them needs a write lock — a read lock admits
  concurrent readers, so two of them could each mutate the same record and the last to dispose would
  win silently.
- **`IsNew` from the strict two is not merely always `false`** — a result arriving with it set is an
  impossible state and throws.

---

## Writing: the store consumes your stream

```csharp
Task<long> WriteAsync(BlobRecord record, Stream content, CancellationToken ct);   // create or replace
Task<long> AppendAsync(BlobRecord record, Stream content, CancellationToken ct);  // append at the end
```

Both return the **resulting total size** — so `AppendAsync` reports the new total, not the number of
bytes you appended.

```csharp
await using var source = File.OpenRead(path);
var size = await manager.DataStore.WriteAsync(record, source, ct);
```

`WriteAsync` is correct whether or not the key already existed, so you never inspect `IsNew` to decide
which method to call. `AppendAsync` takes no offset — the store knows the current size.

### Why not hand out a writable stream?

The obvious shape is `Task<Stream> WriteAsync(record, ct)`: you get a stream, you write, you close it.
This library did exactly that, and changed. Three reasons, in increasing severity.

**The size is unknown while the call runs.** It exists only once you stop writing and flush, which
happens after the store has returned. So the store cannot record it, and something has to go back and
re-read it later.

**The write is not durable until you close the stream.** On a file system that means the recorded size
is briefly wrong. On an object store it means the object *does not exist*: a multipart upload
materialises only at `CompleteMultipartUpload`.

**Disposing the record releases the distributed lock.** This is the serious one. Close the two in the
wrong order and the lock is free while you are still writing. A reader that acquires it then sees a
record whose content is missing — which the reconciliation logic correctly treats as an orphan and
**deletes**. A mis-ordered `Dispose` costs you the record, not merely a wrong number.

And that last one cannot be prevented by the compiler. `Stream` has no way to express "close me before
that other object", so it could only ever be a documented convention. Coupling `BlobRecord` to the
streams opened for it would enforce it, but the record is deliberately a lightweight near-POCO.

Consuming the stream removes all three at once: when the call returns, the bytes have landed and the
size is known. There is no stream left alive, so there is no order to get wrong.

The store is *pulled* at whatever rate your source produces, so this costs nothing in memory or
throughput — a slow store slows your producer down instead of buffering. It is also the shape every
storage SDK already uses: `PutObject` and `BlobClient.UploadAsync` both accept a stream.

### Producing into a stream you are given

Plenty of producers can only write: `JsonSerializer.SerializeAsync`, `XmlWriter`, `StreamWriter`,
`GZipStream` in compress mode. They have no readable form to hand over. For those, `IBlobDataStore`
carries an overload taking the producer instead of the content:

```csharp
await manager.DataStore.WriteAsync(record, (stream, token) =>
    JsonSerializer.SerializeAsync(stream, dto, cancellationToken: token), ct);
```

`AppendAsync` has the same overload. Nothing is buffered either way — how the stream reaches you
depends on the store:

- A store that owns a writable destination, like `FileSystemBlobDataStore`, hands that one over
  directly. No pipe, no extra copy, no second task.
- Any other store inherits a **default implementation** that bridges through `System.IO.Pipelines`:
  your delegate gets the write end of a pipe the store reads from, so back-pressure is preserved and
  a slow store throttles you rather than buffering.

Which means a new backend gets push-style writing for free and can specialise it if it has something
better. Because the two differ, the supplied stream is write-only and you should **not assume it is
seekable** — the file-system store's happens to be, the pipe's is not.

This does **not** bring back the problem above, and the reason is worth being precise about. The
guarantee is not type-level — no signature forbids anything. It is structural, the same way a `using`
block is: the extension owns both ends of the pipe, and **returning from your delegate is the
completion signal**. There is no separate step you could forget, and the store's call still does not
return until the write is finished.

Two things are still on you:

- **Do not return before your writes finish.** Forgetting an `await`, or writing from a task you did
  not wait for, completes the pipe early. This fails loudly — writing after completion throws.
- **Flush your own wrapper.** A `StreamWriter` or `GZipStream` you do not dispose keeps its buffer, and
  those bytes never reach the pipe. This fails *silently*, so dispose it inside the delegate:

```csharp
await manager.DataStore.WriteAsync(record, async (stream, token) =>
{
    // leaveOpen: the pipe is not yours to close. And note Encoding.UTF8 emits a BOM —
    // use a BOM-less encoding unless you actually want those three bytes in the blob.
    await using var writer = new StreamWriter(stream, Utf8NoBom, 1024, leaveOpen: true);
    await writer.WriteAsync(text.AsMemory(), token);
}, ct);
```

A producer that genuinely requires seeking — some image encoders do — must buffer and call the
`Stream` overload directly, since the pipe-backed default cannot offer it.

### Why not offer both?

An `OpenWriteAsync` returning a writable stream would be more comfortable for long or awkward
producing code, and Azure's SDK does ship both directions. It is not offered here because it hands
completion back to the caller and so reinstates all three problems above — and because it would
reintroduce a fork this library already removed once.

That earlier fork was `CreateAsync` versus `WriteAsync`, which differed only in whether the file had to
pre-exist. Choosing correctly required knowing whether the key was new, which the calling code often
did not. One write method that is always right is worth more than two that are each right half the
time.

---

## Reading

```csharp
Task<Stream> ReadAsync(BlobRecord record, CancellationToken ct);
```

The returned stream is **seekable**, which is the contract, not an implementation detail — so reading a
range and resuming a download need no extra method. A backend whose native stream is forward-only
(S3's `GetObject`) has to wrap it; Azure's `OpenReadAsync` already behaves this way.

Reading still hands a stream out rather than consuming one. The asymmetry with writing is deliberate:
a consumer reads at its own pace, and nothing is left uncommitted if it stops early.

---

## Deleting

`DeleteAsync`, `DeleteExpiredAsync`, `DeleteOlderThanAsync` and `CleanupAsync` all remove the content
as well as the metadata, in that order — **content first**.

The order matters. A leftover metadata row is recoverable: it surfaces as `IsNew` and gets pruned on
the next access. A leftover file is invisible to the library and lost for good.

Bulk deletion skips records that are currently locked rather than waiting for them; they are picked up
by the next sweep. `DeleteOlderThanAsync(..., forceDeleteLocked: true)` breaks existing locks instead.

`DeleteAsync` reports failure through exceptions (`KeyNotFoundException`, `TimeoutException`) rather
than a `BlobErrorCode`, because it returns `Task` with nothing to put a code in.

---

## Size, hash, expiration

**`Size`** is owned by the library and read from the data store, never trusted from the metadata row.
It is filled in on every hand-out, and because a handle holds its lock until disposed, that value stays
accurate for the handle's lifetime. Its setter is not public: the library observes the size, callers do
not declare it. `null` means there is no content — a size of `0` is a real, empty blob.

**`Hash`** is the opposite: you declare it through `BlobStoreOptions`, and it is never computed
automatically, because computing it means reading the whole blob.

**Expiration** priority is `AbsoluteExpiration` > `Ttl` > `SlidingExpiration`. A sliding expiration is
persisted and re-applied on each access — reading refreshes `AccessedAt`, writing also `UpdatedAt`.

---

## Changing metadata

A write lock is exclusive, so a record you hold is yours to mutate; whatever you change is persisted
when the handle is disposed. Which setters are open follows what the value *is*: the library owns the
facts it observes (`Size`, `Hash`, the timestamps, `Key`), and you own what only you can know.

```csharp
var (ec, record) = await manager.TryGetForWritingAsync(key, ct);
await using (record)
{
    record.ContentType = "image/png";                                        // state
    record.Apply(new BlobStoreOptions { Ttl = TimeSpan.FromHours(1) });      // instructions
}
```

The two lines are not redundant. `BlobStoreOptions` is the *instruction* type, not a creation-time
convenience: `Ttl` is relative and has nowhere to live on the record, only the values you set are
applied, and expiration follows the priority above. So `Apply` exists for anything that has to be
translated into state, and plain assignment for the rest.

`Apply` needs the write lock, which is also why `TryGetOrSetAsync(key, options, …)` amounts to
get-or-create followed by `Apply`. `TryGetForWritingAsync(key, options, …)` is the same shorthand for
a key that must already exist:

```csharp
var (ec, record) = await manager.TryGetForWritingAsync(key, new BlobStoreOptions { Ttl = TimeSpan.FromHours(1) }, ct);
```

It applies nothing when the acquisition fails, and — unlike `TryGetOrSetAsync`, which persists options
immediately because it may downgrade to a read lock — it writes on dispose along with everything else
you changed. So the handle still has to be disposed for the options to reach storage.

---

## Not supported, on purpose

**Writing at an arbitrary position with the tail preserved.** No object store can do it — there is no
partial overwrite and no `SetLength` — so supporting it on the file system alone would make the
contract unimplementable elsewhere. Resumable upload does not need it: `record.Size` tells you how many
bytes are already stored, and the rest is a plain append.

**Locking inside the data store.** Object stores have no mutual exclusion at all (S3 Object Lock is
WORM retention, not a mutex). Keeping locks in the registry is what keeps the design portable.
