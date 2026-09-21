---
protocol: along
slug: populate-pipeline-and-roundtrip-refactor
type: debt
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [json, populate, performance, reflection, attributes]
milestone: v2.0.0-along-transition
blocked_by: []
related: [core-json-serializer-contract-and-merge-flaws]
---

# Populate Pipeline Feature Deficiencies and Roundtrip Inefficiencies

## Overview
`IJsonSerializer.Populate` is intended to populate existing object instances from JSON. The current implementation in `CoreJsonSerializer.cs` relies on an incomplete custom reflection pipeline that ignores standard STJ serialization attributes and incurs significant performance overhead through per-property round-trip conversions.

## Identified Defects

### 1. Incomplete Member Discovery
In `CoreJsonSerializer.cs` (lines 440-466, `GetOrCreatePropertySetters`):
- `targetType.GetProperties()` is called without binding flags, fetching only public instance properties.
- Only properties with `prop.CanWrite` are processed.
- Public fields are completely ignored, even when `JsonSerializerOptions.IncludeFields = true`.
- Interface documentation states: "Populates the values of the specified JSON string to properties and fields of the target object".

### 2. Ignored Serialization Attributes
`GetOrCreatePropertySetters` determines the property name strictly via:
```csharp
var jsonPropertyName = k.NamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
```
It fails to inspect:
- `[JsonPropertyName("...")]` attribute on properties or fields.
- `[JsonIgnore]` attribute (members marked for exclusion are still populated).
- `[JsonInclude]` or member visibility settings.

### 3. Costly Round-Trip Per Property
In `MergeJsonObjectIntoObject` (lines 491-497):
```csharp
var value = JsonSerializer.Deserialize(
    jsonValue.ToJsonString(),
    propInfo.PropertyType,
    options);
propInfo.Setter(targetObj, value);
```
For every property present in the JSON node, `jsonValue.ToJsonString()` performs a complete string serialization round-trip, followed by `JsonSerializer.Deserialize(string, ...)`. This causes heavy allocations and parses JSON repeatedly.

## Remediation Plan
1. Use `JsonTypeInfo` metadata from STJ resolver where possible or inspect `JsonPropertyNameAttribute`, `JsonIgnoreAttribute`, and fields (`IncludeFields`).
2. Replace `jsonValue.ToJsonString()` + `JsonSerializer.Deserialize(...)` with direct `jsonValue.Deserialize(propInfo.PropertyType, options)` (available in .NET 8+ on `JsonNode`).
3. Align implementation with `IJsonSerializer` XML doc contract to support fields and attribute overrides.

