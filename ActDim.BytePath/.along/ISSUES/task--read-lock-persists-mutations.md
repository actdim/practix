---
protocol: along
slug: read-lock-persists-mutations
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

# read-lock-persists-mutations

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

`UpdateOnReadDisposeAsync` persists the whole record, not just the access timestamps:

```csharp
record.AccessedAt = now;
if (record.SlidingExpiration.HasValue) { record.ExpiresAt = now.Add(...); }
await UpdateRecordAsync(record, CancellationToken.None);
```

So anything a caller changed on a **read**-locked record reaches storage on dispose. A read lock admits
concurrent readers, so two of them can mutate the same record and the last to dispose wins: silently,
with no error and no way for either to notice.

Found while doing #007. The public `BlobRecord.Apply` guards against it (`LockType != Write` throws),
but the plain setters `ContentType`, `Metadata`, `SlidingExpiration` and `ExpiresAt` do not, and they
are public by decision #007 because only the caller can supply those values.

## Design

Two candidate shapes.

**Persist only what a read is allowed to change.** `UpdateOnReadDisposeAsync` writes just
`accessed_at` and `expires_at` instead of the whole row. Narrow, no API change, and it makes the
read path do less work. A caller's stray mutation is then simply dropped rather than persisted -
silent, but harmless, which is the right trade for a read lock.

**Or refuse the mutation outright.** Every settable property checks the lock, like `Apply` does. Loud,
but it puts a guard on a plain property setter, which is unusual and easy to work around by accident
(the record is still a POCO everywhere else). It also means a record obtained for reading cannot be
reused as a plain DTO.

Prefer the first: the read path should not be writing those columns at all, so fixing what it persists
addresses the cause rather than policing the caller. Note the same argument does **not** apply to
`UpdateOnWriteDisposeAsync`: under a write lock persisting the whole record is the intended behaviour
(#007).

## Done when

- [ ] a mutation made under a read lock cannot reach storage
- [ ] the read path persists only the columns a read may legitimately touch
- [ ] test: two concurrent readers mutate the same record, neither change is persisted
- [ ] `AGENTS.md`'s "known hole" note under #007 removed
