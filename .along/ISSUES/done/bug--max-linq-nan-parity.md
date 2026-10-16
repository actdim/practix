---
protocol: along
protocol_version: "4.4.6"
slug: max-linq-nan-parity
type: bug
status: done
completed: 2026-10-06
priority: medium
created: 2026-10-06
updated: 2026-10-06
agent: antigravity
tags: [pkg-practix-common, linq, nan, ieee754, extensions]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Fix MaxOrDefault LINQ Parity for NaN Values

## Problem Statement / Background
In `ActDim.Practix.Common.Extensions.EnumerableExtensions`, `MaxOrDefault` did not follow standard LINQ total ordering semantics for floating-point sequences containing `double.NaN`. Under LINQ standard semantics, `NaN` is considered less than any numeric value for `Max`, meaning `NaN` should only be returned if all elements in the sequence are `NaN`. Previous comparison logic caused order-dependent or non-standard returns when `NaN` was present alongside numeric values.

## Implementation Details
1. Updated `MaxOrDefault` implementation to follow LINQ total ordering rules (`NaN` < any real value).
2. Clarified XML documentation on `MaxOrDefault` to explicitly document LINQ-compatible total ordering semantics for floating-point collections.
3. Updated unit tests in `Tests/Common.Tests/Extensions/EnumerableExtensionsTests.cs` covering permutations of sequences with `NaN` at the start, middle, end, and all-`NaN` sequences.

## Acceptance Criteria
- [x] `MaxOrDefault` returns real maximum values when sequences contain a mix of valid numbers and `double.NaN`.
- [x] `MaxOrDefault` returns `double.NaN` when all elements in sequence are `NaN`.
- [x] XML doc-comments accurately describe LINQ parity behavior.
- [x] Unit tests in `EnumerableExtensionsTests` pass with zero failures.
