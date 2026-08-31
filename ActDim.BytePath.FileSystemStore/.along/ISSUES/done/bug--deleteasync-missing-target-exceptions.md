---
protocol: along
slug: bug--deleteasync-missing-target-exceptions
type: bug
status: done
priority: medium
created: 2026-08-31
updated: 2026-08-31
completed: 2026-08-31
agent: antigravity
tags: [filesystem, bytepath, storage, resilience]
---

# Bug: Handle FileNotFoundException and DirectoryNotFoundException in DeleteAsync

## Problem
In `FileSystemBlobDataStore.DeleteAsync`, if the target file's directory or the file itself is removed concurrently between `File.Exists(path)` and `File.Delete(path)`, `File.Delete(path)` throws `DirectoryNotFoundException` or `FileNotFoundException` instead of returning `false` as specified by the `IBlobDataStore.DeleteAsync` contract. Furthermore, in `PruneEmptyDirectories`, calling `Directory.GetFileSystemEntries` without a `try/catch` block throws `DirectoryNotFoundException` if a directory is deleted concurrently.

## Solution
1. Wrapped `File.Delete(path)` in a `try/catch` catching `FileNotFoundException` and `DirectoryNotFoundException`, returning `false`.
2. Wrapped `Directory.GetFileSystemEntries` and `Directory.Delete` inside `PruneEmptyDirectories` in `try/catch` handling `IOException` and `UnauthorizedAccessException`.
3. Added unit tests in `Tests/BytePath.Tests/FileSystemBlobDataStoreTests.cs` verifying `DeleteAsync` handles missing files and missing directories safely without throwing.
