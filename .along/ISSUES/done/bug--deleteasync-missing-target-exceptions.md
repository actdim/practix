---
protocol: along
protocol_version: "4.4.15"
slug: deleteasync-missing-target-exceptions
type: bug
status: done
priority: medium
created: 2026-09-01
updated: 2026-09-01
completed: 2026-09-01
agent: antigravity
tags: [pkg-bytepath-filesystemstore, filesystem, bytepath, storage, resilience]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Handle FileNotFoundException and DirectoryNotFoundException in DeleteAsync

## Problem Statement / Background
In `FileSystemBlobDataStore.DeleteAsync`, if the target file's directory or the file itself is removed concurrently between `File.Exists(path)` and `File.Delete(path)`, `File.Delete(path)` threw `DirectoryNotFoundException` or `FileNotFoundException` instead of returning `false` as specified by the `IBlobDataStore.DeleteAsync` contract. Furthermore, in `PruneEmptyDirectories`, calling `Directory.GetFileSystemEntries` without defensive exception handling threw `DirectoryNotFoundException` if a directory was concurrently deleted.

## Implementation Details
1. Wrapped `File.Delete(path)` in a `try/catch` catching `FileNotFoundException` and `DirectoryNotFoundException`, returning `false` according to `IBlobDataStore.DeleteAsync` contract.
2. Wrapped `Directory.GetFileSystemEntries` and `Directory.Delete` inside `PruneEmptyDirectories` in `try/catch` handling `IOException` and `UnauthorizedAccessException` to prevent unhandled exceptions during concurrent directory deletions.
3. Added unit tests in `Tests/BytePath.Tests/FileSystemBlobDataStoreTests.cs` verifying `DeleteAsync` handles nonexistent files and directories safely without throwing, and prunes empty directories upon deletion.

## Acceptance Criteria
- [x] `FileSystemBlobDataStore.DeleteAsync` returns `false` instead of throwing when the target file does not exist.
- [x] `FileSystemBlobDataStore.DeleteAsync` returns `false` instead of throwing when the target directory does not exist.
- [x] `PruneEmptyDirectories` catches `IOException` and `UnauthorizedAccessException` during directory pruning.
- [x] Unit tests in `FileSystemBlobDataStoreTests` pass with zero failures.
