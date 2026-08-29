---
slug: debt--stringsplit-regex-cache
type: debt
status: open
priority: low
created: 2026-08-15
updated: 2026-08-15
---

# debt: Cache compiled Regex in StringExtensions.Split

`StringExtensions.Split(expression, delimiter, qualifier, ignoreCase)` creates a `new Regex(pattern, options)` on every call. The pattern depends on `delimiter`, `qualifier`, and `ignoreCase`, so a static field won't work: but a `ConcurrentFactoryDictionary` keyed on `(delimiter, qualifier, ignoreCase)` would amortize the cost for repeated calls with the same arguments.

This matters most for `StringFormatter.FormatHelper` which uses `Regex.Split` as part of every expression parse.
