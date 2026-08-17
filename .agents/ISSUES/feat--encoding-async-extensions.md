---
slug: feat--encoding-async-extensions
type: feat
status: open
priority: low
created: 2026-08-17
updated: 2026-08-17
---

# feat: Async Encoding stream extensions (GetStringAsync and CopyToStreamAsync)

## Problem
`EncodingExtensions` in `ActDim.Practix.Common` currently lacks asynchronous stream decoding (`GetStringAsync`) and stream-to-stream async encoding (`CopyToStreamAsync`).

## Acceptance Criteria
- Add `Task<string> GetStringAsync(this Encoding encoding, Stream stream, CancellationToken ct = default)` overload.
- Add `Task CopyToStreamAsync(this Encoding encoding, string value, Stream destination, CancellationToken ct = default)` overload.
- Add unit tests verifying both methods.
