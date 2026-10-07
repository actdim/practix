---
protocol: along
protocol_version: "4.4.6"
slug: min-max-nan-handling
type: bug
status: done
completed: 2026-10-06
priority: medium
created: 2026-10-06
updated: 2026-10-06
agent: antigravity
tags: [extensions, linq, nan, ieee754]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Fix MinOrDefault and MaxOrDefault NaN Handling and Add ignoreNaN Parameter

## Context
In `EnumerableExtensions.MinOrDefault` and `MaxOrDefault`, when the first element in a sequence is `double.NaN`, subsequent comparison operators `<` and `>` always evaluate to `false` according to IEEE 754, causing `NaN` to stick regardless of subsequent values. When `NaN` is not the first element, it was previously ignored. This created order-dependent behavior.
The LINQ standard behavior is to propagate `NaN` if any element in the sequence is `NaN`.
We align default behavior with LINQ (return `double.NaN` if any element is `NaN`), and introduce an optional `bool ignoreNaN = false` parameter to optionally ignore `NaN` values and compute the min/max among valid numeric values.

## Acceptance Criteria
- [ ] `MinOrDefault` and `MaxOrDefault` propagate `double.NaN` by default when `ignoreNaN` is false.
- [ ] `ignoreNaN` parameter added with default value `false`.
- [ ] When `ignoreNaN` is true, `double.NaN` values are skipped; if all values are `NaN` or sequence is empty, `defaultValue` is returned.
- [ ] XML documentation updated with `<param name="ignoreNaN">`.
- [ ] Unit tests added covering both default LINQ behavior and `ignoreNaN = true` across permutations.
