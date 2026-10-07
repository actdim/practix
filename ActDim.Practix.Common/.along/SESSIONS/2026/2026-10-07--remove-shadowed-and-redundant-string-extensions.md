---
protocol: along
protocol_version: "4.4.6"
date: 2026-10-07
slug: remove-shadowed-and-redundant-string-extensions
agent: antigravity
branch: main
commit: HEAD
summary: 'StringExtensions: remove shadowed Contains/Split/IsNullOrEmpty and rename quote-aware Split to SplitQuoted'
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [debt--remove-shadowed-and-redundant-string-extensions]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Remove shadowed and redundant string extensions

## Summary
StringExtensions: remove shadowed Contains/Split/IsNullOrEmpty and rename quote-aware Split to SplitQuoted.

## Work Completed
- `ActDim.Practix.Common/Extensions/StringExtensions.cs`:
  - Removed `Contains(this string source, string value, StringComparison comparisonType)` which was shadowed by the BCL instance method `string.Contains`.
  - Removed `Split(this string expression, string delimiter)` which was shadowed by BCL `string.Split`.
  - Renamed quote-aware `Split` overloads to `SplitQuoted` to prevent collisions and clearly convey intent.
  - Removed redundant `IsNullOrEmpty(this string value)` extension.
  - Removed dead commented-out regex.
- `Tests/Common.Tests/Extensions/StringExtensionsTests.cs`: added unit tests covering `SplitQuoted` behavior.
- Closed issue `debt--remove-shadowed-and-redundant-string-extensions` as done.

## Decisions
- No new architectural decisions.
