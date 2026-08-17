---
slug: debt--json-serializer-reflectron-optimization
type: debt
status: done
priority: medium
created: 2026-08-17
updated: 2026-08-17
---

# Replace standard reflection in StandardJsonSerializer with fast compiled expression tree setters

## Context
`StandardJsonSerializer.MergeJsonObjectIntoObject` used standard un-cached reflection (`Type.GetProperties()` and `PropertyInfo.SetValue`) during JSON population/merging. This caused reflection overhead on property updates.

## Objectives
- Refactor `MergeJsonObjectIntoObject` to use fast compiled expression tree property setters (`Expression.Lambda<Action<object, object>>`).
- Cache property metadata and compiled setters per `(Type, NamingPolicy)` using `ConcurrentDictionary` to avoid repeated reflection lookups.
- Ensure 100% test passing across the solution (467/467 tests passed).
