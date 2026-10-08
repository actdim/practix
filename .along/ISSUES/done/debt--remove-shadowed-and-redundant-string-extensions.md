---
protocol: along
protocol_version: "2.2.26"
slug: debt--remove-shadowed-and-redundant-string-extensions
type: debt
status: done
priority: low
created: 2026-10-07
updated: 2026-10-07
completed: 2026-10-07
agent: antigravity
tags: [string-extensions, api-cleanup, technical-debt, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: [debt--stringsplit-regex-cache]
---

# Remove Shadowed and Redundant Methods from StringExtensions

## Overview
Clean up `ActDim.Practix.Common/Extensions/StringExtensions.cs`:
1. Remove `Contains(this string source, string value, StringComparison comparisonType)` which is shadowed by BCL instance method `string.Contains(string, StringComparison)`.
2. Remove `Split(this string expression, string delimiter)` which is shadowed by BCL instance method `string.Split(string?, StringSplitOptions)`.
3. Rename the remaining quote-aware `Split` overloads to `SplitQuoted` to resolve naming collision with standard `string.Split` and clarify semantic intent.
4. Remove `IsNullOrEmpty(this string value)` which is an unnecessary wrapper around static `string.IsNullOrEmpty` and lacks compiler flow analysis attributes (`[NotNullWhen(false)]`).
5. Remove commented-out regex line `//\s+(?=(?:[^"]*"[^"]*")*(?![^"]*"))` per coding standards.

## Requirements
1. Remove `Contains`, `IsNullOrEmpty`, and single-delimiter `Split`.
2. Rename quote-aware `Split` overloads to `SplitQuoted`.
3. Verify test suite passes without regressions.
