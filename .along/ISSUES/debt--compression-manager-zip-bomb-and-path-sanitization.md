---
protocol: along
slug: compression-manager-zip-bomb-and-path-sanitization
type: debt
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [compression, security, zip-bomb, path-traversal, sanitization, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: [debt--compression-interface-cleanup]
---

# CompressionManager Unbounded Decompression Limits and Archive Path Sanitization

## Overview
`CompressionManager.cs` provides stream and archive compression. While resource pooling and ownership are generally well handled, static analysis identified a lack of payload expansion limits during decompression (vulnerability to zip/decompression bombs) and missing path normalization on archive entry names.

## Identified Defects

### 1. No Maximum Decompressed Size Limits (Zip Bomb Vulnerability)
In decompression routines across `CompressionManager.cs`:
Streams are decompressed until EOF without checking cumulative decompressed bytes or enforcing an expansion ratio threshold. Maliciously crafted compressed streams (e.g. gzip/brotli/zip bombs with extreme compression ratios) can inflate into gigabytes or terabytes of decompressed data, exhausting disk, memory, or thread pool resources.

### 2. Archive Entry Name (`FullName`) Normalization
When processing archive entries (`IArchiveEntry`, `ZipArchiveEntry`), `entry.FullName` originates directly from archive metadata. Although direct disk extraction is not currently performed inside `CompressionManager`, consumers querying or resolving entry names receive un-normalized paths containing relative navigation segments (`../`, `..\`) which risks Zip Slip path traversal if downstream code saves entries to the filesystem.

## Remediation Plan
1. Add an optional or configurable `maxDecompressedBytes` parameter / safety limit to `DecompressAsync` methods that throws `InvalidDataException` or `IOException` if decompression exceeds the ceiling.
2. Sanitize and normalize entry path names in archive inspection routines, stripping directory traversal characters.
3. Link with `debt--compression-interface-cleanup` for general archive/compression API enhancements.

