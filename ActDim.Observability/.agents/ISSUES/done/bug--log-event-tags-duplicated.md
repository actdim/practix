---
slug: log-event-tags-duplicated
type: bug
status: done
priority: high
created: 2026-08-15
updated: 2026-08-15
---

# Bug: `LogEvent` Writes Every Tag Twice Under Two Names

## Description
`EnrichSpanFromScope` first flattens the whole `LogEvent`, which walks its `ActivityTags` property as a nested dictionary and yields `activity.tags.<name>`, and then iterates `ActivityTags` again, yielding the same values under their plain names. Every tag lands on the span twice.

Observed on a real span:

```
tags: name=ImportBatch, activity.tags.priority=5, priority=5
```

Half of the attributes are noise, and they double the exported payload for the most common scope state type in the package.

## Proposal
Flatten only the domain-meaningful part of `LogEvent` — the `Name` — and let the explicit `ActivityTags` loop own the tag names, or exclude `ActivityTags` from the reflection walk. The `activity.tags.` prefix has no reason to exist in the exported data.

## Acceptance
- [x] A `LogEvent` scope produces one attribute per entry of `ActivityTags`.
- [x] `name` is still written.
- [x] A test asserts the absence of the `activity.tags.` prefix.
