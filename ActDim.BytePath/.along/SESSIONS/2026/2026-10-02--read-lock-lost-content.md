---
protocol: along
protocol_version: "4.4.6"
date: 2026-10-02
slug: read-lock-lost-content
agent: antigravity
branch: main
commit: HEAD
summary: 'BlobManager: reacquire write lock when content was lost externally under read lock'
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [bug--read-lock-lost-content]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Read lock lost content

## Summary
BlobManager: reacquire write lock when content was lost externally under read lock.

## Work Completed
- `BlobManager.cs`:
  - Updated `ReconcileContentAsync` to reacquire a `LockType.Write` lock when an existing record's content was deleted/lost externally, allowing `TryGetOrSetAsync` callers with `LockType.Read` to safely recreate the content without triggering `EnsureWriteLock` errors.
  - Preserved caller options during lock reacquisition.
- `Tests/BytePath.Tests/BlobManagerTests.cs`:
  - Added `TryGetOrSetAsync_ContentLostExternally_WithReadLockType_ReacquiresWriteLockAndAllowsWriting`.
  - Added `TryGetOrSetAsync_ContentLostExternally_WithReadLockType_PreservesOptions`.
- Marked `bug--read-lock-lost-content` as done.

## Decisions
- No new architectural decisions.
