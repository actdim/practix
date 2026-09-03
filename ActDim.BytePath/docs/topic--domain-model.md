---
protocol: along
protocol_version: "2.2.8"
slug: domain-model
title: Domain Model & Vocabulary
type: domain-model
created: 2026-08-31
updated: 2026-09-02
tags: [domain]
---

# Domain Model & Vocabulary

Core domain terminology, data models, and state transitions.

- **Record/Content invariants**: A record without content is transient and only `TryGetOrSetAsync` may observe it, via `IsNew`; `Size` is library-owned and always read from the store; `BlobRecord` stays decoupled from streams; deletion removes content before metadata.
- **Write invariants**: The write surface has no position and no mode flags; **writes consume a stream, reads hand one out**; options are instructions and the record is state, so `BlobRecord.Apply` translates one into the other under a write lock: and `TryGetForWritingAsync(key, options, ...)` is that acquire-then-`Apply` pair as one call, persisting on dispose (#008).
- **Traps**: Lock acquisition is not re-entrant: hence no self-locking delete in the registry. `TimeSpan.Zero` as a lock timeout means "try once"; only a negative value means "unspecified". Read streams are promised seekable; the producer-delegate stream is **not** promised seekable.
- **IBlobDataStore constraints**: Cannot enumerate its content, so nothing can find orphaned files or audit the store against the registry: see `integrity-audit`.
