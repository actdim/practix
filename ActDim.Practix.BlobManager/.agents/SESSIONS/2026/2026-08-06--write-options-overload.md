---
date: 2026-08-06
slug: write-options-overload
agent: Claude Code / claude-opus-5[1m]
branch: main
commit: 560da43
summary: TryGetForWritingAsync now accepts BlobStoreOptions, applied to the handed-out record and persisted on dispose (#008)
---

## What changed & why

#007 made `BlobRecord.Apply` public so an existing blob's metadata could be changed under the write
lock it was handed out with, but the acquiring call itself still took no options — only
`TryGetOrSetAsync` did. Updating an existing blob therefore always read as acquire-then-apply while
the create-or-update path read as one call, purely because of which overload existed.

Added two overloads mirroring `TryGetOrSetAsync`'s parameter order:

```csharp
Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, CancellationToken ct);
Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, TimeSpan timeout, CancellationToken ct);
```

They are the existing overloads plus `record.Apply(options)`, via a private `BlobManager.ApplyOptions`
that returns a failed result untouched and treats a null `options` as a no-op.

The one asymmetry worth knowing: **the options are persisted on dispose, not immediately.**
`TryGetOrSetAsync` writes them straight away because it may downgrade to a read lock and so has to
persist while it still holds the write lock. `TryGetForWritingAsync` holds the write lock for the
handle's lifetime and dispose persists the record anyway, so writing earlier would buy nothing.

Not extended to `TryGetForReadingAsync`: `Apply` requires a write lock, and the read-dispose hole
tracked by `read-lock-persists-mutations` is a reason not to widen that path.

`IBlobRegistry` and `SQLiteBlobRegistry` are untouched — applying options is computation over a
record (#007), so it belongs in `BlobManager`.

## Files touched

- `IBlobManager.cs` — the two overloads + XML doc on the persist-on-dispose behaviour
- `BlobManager.cs` — implementation and `ApplyOptions`
- `Tests/BlobManager.Tests/BlobManagerTests.cs` — 3 tests: metadata/TTL persisted on dispose, missing
  key still `KeyNotFound`, timed-out acquisition applies nothing
- `README.md` — the shorthand added to "Changing metadata"
- `AGENTS.md` — invariant #7 note, test count 41 → 64 (it was stale)
- `.agents/DECISIONS.md` (#008), `.agents/CONTEXT.md`

## Tasks advanced

None — this closes the gap #007's context called out; no task file covered it.

## Gaps / follow-ups

- The sibling `CanarySystems.FileStorage` copy has **not** received this change yet.
- `read-lock-persists-mutations` unchanged and still the reason reading takes no options.
