---
date: 2026-08-06
slug: record-mutation-contract
agent: Claude Code / claude-opus-5[1m]
branch: main
commit: 4995d1e
summary: >
  Settled what a caller may write on a BlobRecord and why, moved ApplyOptions out of the registry onto
  the record as a public write-locked Apply, and trimmed rationale out of the public XML docs.
---

# What a caller may write on a record, and where rationale belongs

Follows `write-direction-inversion` the same day. Three separate things, all triggered by review remarks
rather than planned work.

## Rationale does not belong in public XML docs

The override of the producer-form `WriteAsync` in `FileSystemBlobDataStore` carried a `<summary>`
explaining why it does not use the pipe the default implementation would set up. That is an
implementation detail on a public member, so it reaches consumers through IntelliSense, and it argues
about an alternative that was not chosen: which is `DECISIONS.md`'s job, where #006 already covers it.

Removed, and the same pass trimmed `IBlobDataStore`'s remarks down to what a caller has to know to use
the methods correctly: the write is complete on return, returning from the producer delegate completes
the content, the supplied stream is write-only and not to be assumed seekable, and a wrapper around it
must be flushed before returning. The mechanism (pipes, who wraps what) came out. `ReadAsync` lost the
"a forward-only backend has to wrap it" clause: that is a requirement on implementations, so it lives
in `AGENTS.md`.

The split now in force: XML docs say what a member does for the caller; `DECISIONS.md` says why the
shape was chosen; `AGENTS.md` states requirements on implementations; `README.md` is the one place that
deliberately explains rationale, because it was written for that.

## Setter visibility, and why mutation is not the problem (#007)

`BlobRecord` had public setters for everything except `Size`, which looked inconsistent. It was not:
a write lock is exclusive, so mutating a record you hold and having it persisted on dispose is what the
lock is *for*. Locking the setters down wholesale would have removed the only way to change an existing
blob's metadata: `TryGetForWritingAsync` takes no options.

So the rule is about what a value **is**, not whether mutation is safe:

- `internal`: facts the library observes: `Size`, `Hash`, `CreatedAt`/`UpdatedAt`/`AccessedAt`, `Key`.
  A caller can only lie about these.
- public: intent only the caller can supply: `ContentType`, `Metadata`, `SlidingExpiration`,
  `ExpiresAt`. The library cannot derive them.

`Hash` closing now matters for `content-hash`: the `internal` setter becomes where the library puts the
value it *computed*, instead of storing an unchecked claim.

## `ApplyOptions` → `BlobRecord.Apply` (#007)

The real defect was asymmetry: options applied through `TryGetOrSetAsync` but not through
`TryGetForWritingAsync`, though the lock is identical. Fixed by moving the method onto the record:

- `public void Apply(BlobStoreOptions)`: requires the write lock.
- `internal void Apply(BlobStoreOptions, DateTimeOffset now)`: used by the registry, which applies
  options while `LockType` is still undecided and needs its own `now` so all timestamps in one
  operation agree.

This also names the roles: `BlobStoreOptions` is the **instruction** type, the record is **state**.
`Ttl` is relative and has nowhere to live on the record; "apply only what was set" and the
AbsoluteExpiration > Ttl > SlidingExpiration priority are rules. Replacing options with plain setters -
which was the alternative considered: would push that translation onto the caller.

## Files touched

- `BlobRecord.cs`: setter visibility, `Apply` (public + internal overload)
- `SQLiteBlobRegistry.cs`: `ApplyOptions` removed, call site now `record.Apply(options, now)`
- `IBlobDataStore.cs`, `FileSystemBlobDataStore.cs`: doc trimming
- `README.md`: new "Changing metadata" section; `AGENTS.md`: new invariant §7
- `Tests/BlobManager.Tests/BlobManagerTests.cs`: 4 `Apply` tests (54 total)

## Decisions

#007 (new). #002's `Hash` note is superseded in part: the setter is no longer public.

## Tasks

- `content-hash`: rewritten with the two ideas raised this session: a multipart-style write session
  that verifies a caller-declared hash at completion, and incremental hashing. Recorded the hard
  constraint that neither `IncrementalHash` nor `XxHash3` exposes its state, so a hasher cannot be
  persisted between independent `AppendAsync` calls: which is what forces either a session or clearing
  the hash. Also flagged that `Hash` has no algorithm tag, so verification is currently unimplementable.
- `read-lock-persists-mutations`: new, found while doing #007.
- `url-safe-key-separator`: new. `/` in a key means directories, but the key is also the public ID and
  a slash breaks URL routing (`%2F` is not a reliable escape through proxies). The separator choice is
  still open; the collision defect found alongside it **was fixed**: `SanitizeFileName` mapped many
  characters onto `_`, so `dir/a:b` and `dir/a_b` shared a file in the hash-free multi-segment branch.
  Replaced by reversible `%XX` escaping, `/` is now the only separator, and trailing dot/space are
  escaped because Windows trims them. Seven tests; test count 54 → 61.

## Gaps / follow-ups

- `UpdateOnReadDisposeAsync` persists the whole record, so a mutation made under a **read** lock reaches
  storage and two concurrent readers can race. `Apply` guards, the plain setters cannot. Filed.
- `di-registration` is still the blocker for any consumption outside the assembly.
