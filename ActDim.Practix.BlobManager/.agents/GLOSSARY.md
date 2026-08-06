# Glossary

_Domain terms. Add a term when you introduce or clarify it._

- **Record** — the metadata row for a key in `blob_records`. Exists independently of the content:
  creating one reserves the key and takes a lock, nothing more.
- **Content** — the stored bytes, owned by `IBlobDataStore`. The registry never sees them.
- **Orphaned record** — a record whose content is absent. A transient state by decision #001:
  `TryGetOrSetAsync` reports it through `IsNew`, `TryGetFor*` deletes it and returns `KeyNotFound`.
- **Reconciliation** — `BlobManager.ReconcileContentAsync`: bringing a record and its actual content
  into agreement on hand-out. Mutating, not a check — it fills `Size`, may set `IsNew`, may delete.
- **`IsNew`** — "there is no content yet". Covers both a record the registry has just created and
  one that outlived its content; **not** "the record was just inserted".
- **Handle** — a successful `BlobResult` / `BlobRecord` pair that holds a lock until disposed. Its
  `Size` stays authoritative for its lifetime, since no one else can write meanwhile.
- **Shard directory** — the two subfolders `FileSystemBlobDataStore` derives from the key's
  `XxHash3` so flat keys do not pile into one directory. Hashes the key, never the content.
- **Forced deletion** — `forceDeleteLocked`: break existing locks (`ForceUnlockAsync`) and delete
  anyway, instead of skipping locked records.
- **Producer form** — the `WriteAsync`/`AppendAsync` overload taking
  `Func<Stream, CancellationToken, Task>` instead of the content. For producers that can only write.
  Returning from the delegate is what completes the write, so completion cannot be forgotten.
- **Write-only producer** — an API that writes into a stream it is handed and offers no readable form
  (`JsonSerializer.SerializeAsync`, `XmlWriter`, `GZipStream` in compress mode). The reason the
  producer form exists.
- **Bridge** — `ProducerStreamBridge`: the default behind the producer form, pairing a `Pipe`'s write
  end for the producer with its read end for the store. A store owning a writable destination
  overrides it instead.
