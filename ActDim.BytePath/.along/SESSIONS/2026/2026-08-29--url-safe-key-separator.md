---
protocol: along
date: 2026-08-29
slug: 2026-08-29--url-safe-key-separator
agent: antigravity
branch: main
commit: unknown
summary: Work session log.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

﻿---
date: 2026-08-29
slug: url-safe-key-separator
agent: Antigravity / Gemini 3.7 Flash
branch: main
commit: pending
summary: Standardized on ':' URL-safe key separator per RFC 3986 pchar, added configurable HierarchySeparator, escaped Windows reserved device names, and stripped KeyPrefix in FileSystemBlobDataStore.
---

## What changed

- **URL-safe Logical Hierarchy Separator:**
  - Standardized on `:` (colon) as the default logical separator for hierarchical namespaces per RFC 3986 `pchar` (`pchar = unreserved / pct-encoded / sub-delims / ":" / "@"`).
  - Keys with `:` round-trip cleanly in REST endpoints (`/api/blobs/{key}`) without wildcard catch-all route workarounds or percent-encoding pitfalls.
- **Configurable `HierarchySeparator`:**
  - Added `FileSystemBlobDataStoreOptions.HierarchySeparator` (char?, default `':'`).
  - When set, multi-segment keys split into physical directories; single-segment flat keys use uniform 2-level `XxHash3` sharding.
  - Setting `HierarchySeparator = null` disables folder splitting and routes all keys through hash-sharded buckets.
- **`KeyPrefix` Stripping in Physical Paths:**
  - `FileSystemBlobDataStore.BuildPath` strips its own `KeyPrefix` from the key before computing physical paths so storage backends do not create redundant subdirectories (e.g. `_basePath/reports/...` instead of `_basePath/fs/reports/...`).
- **Windows Reserved Device Name Escaping:**
  - `EscapeFileName` detects DOS/Windows reserved names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`, with or without extensions) and escapes the leading character (e.g. `con.txt` -> `%63on.txt`) to avoid Win32 device handle traps.
- **Documentation & Protocol Synchronization:**
  - Updated `ActDim.BytePath/README.md` and `ActDim.BytePath.FileSystemStore/README.md` with Key Format and Storage Layout sections.
  - Recorded ADR #011 in `DECISIONS.md`.
  - Moved `task--url-safe-key-separator.md` to `done/` and updated `ISSUES.md`, `CONTEXT.md`, and `HISTORY.md`.

## Verification

- `dotnet test Tests/BytePath.Tests/ActDim.BytePath.Tests.csproj -v q`
- Result: 101 passed, 0 failed, 0 skipped (100% success rate).

## Files touched

- `ActDim.BytePath.FileSystemStore/FileSystemBlobDataStore.cs`
- `ActDim.BytePath.FileSystemStore/FileSystemBlobDataStoreOptions.cs`
- `ActDim.BytePath.FileSystemStore/README.md`
- `ActDim.BytePath/README.md`
- `Tests/BytePath.Tests/BlobManagerTests.cs`
- `ActDim.BytePath/.agents/CONTEXT.md`
- `ActDim.BytePath/.agents/DECISIONS.md`
- `ActDim.BytePath/.agents/ISSUES.md`
- `ActDim.BytePath/.agents/ISSUES/done/task--url-safe-key-separator.md`
- `ActDim.BytePath/.agents/HISTORY.md`
- `ActDim.BytePath/.agents/SESSIONS/2026/2026-08-29--url-safe-key-separator.md`
