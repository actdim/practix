# url-safe-key-separator

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

`FileSystemBlobDataStore.BuildPath` treats `/` and `\` in a key as a request for directories:

```csharp
var segments = (key ?? string.Empty).Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
if (segments.Length > 1) { /* preceding segments become subfolders */ }
```

But the key is also the blob's **public identifier**, and identifiers get passed through URLs. A key
containing `/` breaks that:

- A route like `blobs/{key}` will not match `blobs/report/2026/x.pdf` — segment binding stops at the
  slash. It needs a catch-all `{*key}`, which changes matching semantics and collides with sibling
  routes.
- Percent-encoding is not a reliable escape. `%2F` inside a path segment is normalised, decoded early or
  rejected outright depending on the server and any proxy in front of it, so a key that survives locally
  can break in deployment.
- Moving the key to the query string works but changes the shape of the API for the sake of a storage
  detail.
- `\` has the same problem from the other side: some servers fold it into `/`.

## The deeper issue: two separators are being conflated

There is a **logical** separator — how callers group keys, which `QueryAsync` patterns already rely on
(the tests use `qry:a`, `qry:b` and match `qry:*`) — and a **physical** one, which the file-system store
uses to decide where directories go. Right now the file-system store dictates the logical key format for
everybody: choose `/` in your keys and you get directories, whether you wanted them or not.

That is backwards, and it does not survive a change of backend. On S3 there are no directories at all;
`/` in a key is purely a listing-prefix convention. So which character means hierarchy is a decision for
the store, not a property of the key.

## Options

| separator | URL-safe | note |
|---|---|---|
| `~` | yes — unreserved in RFC 3986 | needs no encoding anywhere; some servers give `~` meaning at the *start* of a segment (user dirs), so avoid leading `~` |
| `--` | yes | ambiguous against a key that legitimately contains `--` |
| `:` | yes in a path segment (`pchar` allows it) | **already used as the logical separator in tests**, but invalid in Windows filenames, so `SanitizeFileName` already mangles it to `_` |
| configurable | depends on the choice | most flexible, but the separator becomes part of the on-disk layout — see migration below |

Whatever is chosen, `/` and `\` should stop being magic in the key. Either the store takes the separator
as configuration, or hierarchy stops being derived from the key at all and becomes explicit.

**Migration is the real cost.** The separator and the shard layout together determine where every
existing blob lives. Changing either orphans the content already on disk, and there is no re-layout
tooling — the registry would still resolve keys, but `BuildPath` would point somewhere else. Any change
here needs either a migration pass or a layout version recorded alongside the data.

## Adjacent defect — FIXED 2026-08-06

Sanitisation made two distinct keys collide. `SanitizeFileName` mapped every character invalid in a
filename to `_`, and the **multi-segment** branch has no hash in the path — it is
`_basePath / Sanitize(seg0) / Sanitize(seg1) / …` — so `a/b:c` and `a/b_c` resolved to the same file and
the second write silently destroyed the first. The flat-key branch was safe only by accident, saved by
the two shard directories derived from the `XxHash3` of the raw key.

Fixed by making the mapping **reversible** rather than lossy — the cause was information loss, not the
absence of a hash:

- `SanitizeFileName` → `EscapeFileName`: anything a filename cannot carry becomes `%XX`, and `%` itself
  is escaped so an escaped form cannot be forged. Distinct segments can no longer produce one name.
- A trailing `.` or space is escaped too, because Windows silently trims those and would alias `a.`
  onto `a`.
- The key now splits on `/` **only**. A backslash is an ordinary escaped character, so `a\b` stays a
  distinct key instead of aliasing onto `a/b`.

Covered by `DistinctKeys_NeverShareContent` (six cases, each of which fails on the old behaviour) and
`Key_WithSeparators_KeepsItsFileExtension`, which pins that escaping does not disturb an ordinary name —
the extension has to survive for `ResolveLocationAsync` to stay useful.

### Residual aliasing, deliberately left

- `a//b` and `a/b` still resolve to the same path: the split uses `RemoveEmptyEntries`, so repeated
  separators normalise. Closing it means deciding whether an empty segment is an error, which belongs
  with the separator decision below rather than before it.
- Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`) are not
  escaped. That is a *failure* rather than a collision — creating such a file throws — and it needs a
  different remedy from escaping, since the whole name is reserved rather than a character in it.

## Done when

- [ ] `/` and `\` are no longer implicitly meaningful in a key, or the separator is explicit configuration
- [ ] a key that round-trips through a URL path segment without encoding is documented as the supported
      shape, with the chosen separator stated
- [x] distinct keys cannot resolve to the same path, including in the multi-segment branch (2026-08-06)
- [ ] repeated separators (`a//b`) and Windows reserved device names are handled — see residuals above
- [ ] the layout is versioned, or a migration path is written down — changing it must not silently orphan
      existing content
- [ ] `README.md` states what characters a key may contain and why
