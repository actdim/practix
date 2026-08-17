# integrity-audit

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

Nothing ever checks that a blob's bytes still match what the registry says about them. Content lives on
a file system that anything with write access can touch — a person with a shell, a backup restore, a
sync client, another process. After such an edit the registry and the data store disagree and the
library carries on as if nothing happened.

What can actually drift, precisely:

- **`Hash` — always wrong-able, and never checked.** Nothing computes or verifies it today, so it is a
  caller's claim, not a fact (see `content-hash`). Once hashing exists, an out-of-band edit is exactly
  the case only a re-read can detect: the bytes changed, the recorded hash did not.
- **`Size` — drifts only in the stored column.** `ReconcileContentAsync` re-reads the real size on every
  hand-out and `TrackSizeOnDispose` refreshes it after a write, so a handle never shows a stale value.
  The `blob_records.size` column can go stale if nobody takes a handle after the edit, but nothing
  public reads that column — `QueryAsync` returns keys only, and the expiry sweeps read `expires_at`. So
  today this half is self-healing and low priority; it becomes real as soon as reporting is built
  directly on the registry.
- **Orphaned content — undetectable.** Files with no registry row. Any store that ran the pre-#004 code
  has them, because deletion used to remove metadata only. Nothing looks for them and nothing can.

## Blocker: the content side cannot be enumerated

`IBlobDataStore` has `GetSizeAsync`, `ReadAsync`, `WriteAsync`, `AppendAsync`, `DeleteAsync`,
`ResolveLocationAsync` — and no way to **list** what it holds. So "compare what is stored against what
is registered" is not implementable in either direction: you can walk the registry and check each key,
but you cannot walk the content and find rows that should exist.

An `EnumerateAsync` (or an async-enumerable of keys) has to come first. Note it maps cleanly onto every
backend — `ListObjectsV2` on S3, `GetBlobsAsync` on Azure, a directory walk on a file system — and it is
also what a future `QueryAsync`-over-content would need.

## Design

**Scope per pass.** Hashing every blob is O(total bytes), so a full sweep is not something to run on
startup or on a timer over a large store. Wants to be incremental: sampled, or ordered by
`accessed_at`/`updated_at` so the least recently verified go first, with a budget per pass.

**Locking.** Each key must be verified under a **read** lock, or the audit races a legitimate writer and
reports its own torn read as corruption. For an orphaned file there is no record and therefore no lock
to take, so orphan removal is inherently racy against a concurrent `TryGetOrSetAsync` creating that very
key — probably only remove orphans older than some threshold, the same way an S3 lifecycle rule handles
abandoned multipart uploads.

**Policy on mismatch is the decision to get right, and it must be explicit.** Three choices, and each
destroys something if picked silently:

- *trust the bytes* — update `Size`/`Hash` to match the file. Loses the evidence that a tamper happened.
- *trust the metadata* — delete the blob as corrupt. Destroys data that may have been edited on purpose.
- *report only* — quarantine the finding and let a human decide. The only safe default.

So the audit reports; repair is a separate, opted-into action. `CleanupAsync` today releases expired
locks and deletes expired records — the audit does **not** belong inside it, because cleanup is expected
to be safe to run unattended and this is not.

## Done when

- [ ] `IBlobDataStore` can enumerate what it holds
- [ ] an audit operation reports, per key: size mismatch, hash mismatch (once `content-hash` lands),
      registered-but-missing content, and content with no registry row
- [ ] verification takes a read lock per key, so a concurrent writer is never reported as corruption
- [ ] the pass is bounded — sampled or budgeted — rather than always walking everything
- [ ] mismatches are reported, never silently repaired; repair is a separate explicit operation
- [ ] orphan removal only considers content old enough not to be a blob being created right now
- [ ] tests that corrupt content behind the library's back and assert each mismatch kind is reported
