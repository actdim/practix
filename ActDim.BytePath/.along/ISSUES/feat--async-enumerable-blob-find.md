---
protocol: along
slug: async-enumerable-blob-find
type: feat
status: open
priority: medium
created: 2026-08-14
updated: 2026-08-14
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Feature: IAsyncEnumerable Support for IBlobStorage.FindAsync

## Description
Support streaming blob search results via `IAsyncEnumerable<IBlob>` in `IBlobStorage.FindAsync` using `[EnumeratorCancellation] CancellationToken` instead of buffering all matches into `Task<IList<IBlob>>`.

## Key Requirements
1. Add `IAsyncEnumerable<IBlob> FindAsync(string pattern, [EnumeratorCancellation] CancellationToken cancellationToken = default)` interface method to `IBlobStorage`.
2. Implement streaming enumeration across blob storage backends (filesystem, memory, S3/Azure blobs).
3. Ensure proper cancellation support during async stream iteration.
