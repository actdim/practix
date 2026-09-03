---
protocol: along
slug: content-hash
type: task
status: open
priority: medium
created: 2026-08-29
updated: 2026-08-29
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# content-hash

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

`BlobRecord.Hash` is stored in the schema and exposed publicly, but nothing ever computes it. The only
way it gets a value is a caller declaring it through `BlobStoreOptions.Hash`: which means the field is
a claim nobody checks, not a fact. Decision #002 left it that way deliberately: computing a hash meant
reading the whole blob back, and that must not happen implicitly on every write.

That reason is gone. Since #006 the bytes flow **through** the store on their way to disk
(`WriteThroughAsync`), so a hash can be computed in the same pass at no extra read. The helper is even
already written: `FileSystemBlobDataStore.ComputeXxHash3Async` sits commented out at the bottom of the
file.

Two things are wanted, and they are separable:

1. **A correct stored hash**: computed by us, not taken on trust.
2. **Verification**: the caller declares an expected hash and we confirm the stored bytes match it.
   This is the more valuable half: it turns a silently corrupted transfer into an error.

## Phase 1: compute and verify on a single-shot write

Cheapest and covers most of the value. While `WriteThroughAsync` streams the bytes, hash them; at the
end, if the caller declared an expected hash, compare and **fail** on mismatch.

Failing has to mean something. `FileMode.Create` has already truncated the file by then, so on mismatch
the content must be deleted rather than left in place: otherwise a rejected write leaves a
partially-correct blob that reconciliation (#001) will happily report as valid with the wrong size.

## Phase 2: a multi-step write session

`AppendAsync` can be called repeatedly, and each call commits independently: there is no point at which
"the whole blob is now written" is known, so there is nowhere to compute or check a whole-blob hash.
A session would provide that point:

```
BeginWriteAsync(record)  →  Append... Append...  →  CompleteAsync(expectedHash)
```

`CompleteAsync` computes the full hash, compares it with what the caller declared, stores it, and
returns the verified size. Not completing means not committing.

This shape is not invented for us: it is what the backends already have. S3: `CreateMultipartUpload` /
`UploadPart` / `CompleteMultipartUpload`, with per-part and whole-object checksums
(`x-amz-checksum-sha256`, composite for multipart). Azure: staged blocks plus `Content-MD5` on the
block and on the blob. So a session with a completion checksum maps directly onto both.

**The tension to resolve before building it.** A session hands the commit step back to the caller,
which is exactly what #006 removed for single-shot writes: and #006's reasoning was that a caller who
forgets to finish leaves a missing object with the lock already released. A multi-step write cannot
avoid having a commit step, so the question is not whether to have one but how to keep forgetting it
harmless. Note the trade-off inverts: today each `AppendAsync` commits immediately, so a crash halfway
leaves a truncated blob that looks valid; with a session a crash leaves nothing committed, which is
safer. Decide deliberately, and record it: this is an ADR, not an implementation detail.

## Incremental hashing, and why state cannot be persisted

Both candidate algorithms support incremental use:

- `System.Security.Cryptography.IncrementalHash`: `AppendData` / `GetHashAndReset`.
- `System.IO.Hashing.XxHash3`: `Append` / `GetCurrentHash`. Already referenced by this project.

But **neither exposes its internal state**, so the hasher cannot be serialised and resumed. That is the
constraint that decides the design:

- Within one session or one call, incremental hashing works and costs nothing extra.
- **Across independent `AppendAsync` calls it is impossible.** There is nowhere to keep the hasher -
  putting it in the registry would require exporting state that the BCL does not let us export.
  (SHA-256 state is only eight words plus a buffer, so this is an API limitation rather than a
  mathematical one; getting at it would mean a hand-rolled or third-party implementation, which is not
  worth it here.)

So the options for a plain `AppendAsync` outside a session are: re-read the whole blob (the cost #002
refused), or **clear the hash** and let it be recomputed on demand. Clearing is the honest default; a
stale hash is worse than none.

## Which algorithm, and the missing algorithm tag

Two different purposes, and the choice is not the same for both:

- integrity and dedup: `XxHash3` is fast and adequate, and is already in the project.
- content addressing or anything security-facing: SHA-256.

`BlobRecord.Hash` is a bare string with **no record of which algorithm produced it**, so two callers
using different ones write values that cannot be compared or verified against each other. Storing the
algorithm alongside: or fixing one per store and stating it: has to be settled as part of this task,
otherwise verification cannot be implemented at all.

Note the existing `XxHash3` use in `FileSystemBlobDataStore` hashes the **key**, for path sharding. It
is unrelated to content hashing and the naming must not blur the two.

## Done when

- [ ] hashing is opt-in and computed during the write, with no second pass over the content
- [ ] the stored hash records which algorithm produced it, or the algorithm is fixed and documented
- [ ] a declared expected hash is verified, and a mismatch fails the write **and** removes the content
- [ ] plain `AppendAsync` outside a session clears the hash rather than leaving it stale
- [ ] the multi-step session is either built with its commit-ownership trade-off recorded as an ADR, or
      explicitly deferred with the reason written down
- [ ] the commented-out `ComputeXxHash3Async` is either used or removed
- [ ] `README.md`'s "Hash is never computed automatically" paragraph updated
