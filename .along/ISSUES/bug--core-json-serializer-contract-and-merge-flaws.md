---
protocol: along
slug: core-json-serializer-contract-and-merge-flaws
type: bug
status: open
priority: high
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [json, serialization, bug, merge, options, pkg-practix-json]
milestone: v2.0.0-along-transition
blocked_by: []
related: [debt--populate-pipeline-and-roundtrip-refactor, debt--stj-converters-and-newtonsoft-incompatibilities]
---

# CoreJsonSerializer Contract and Merge Flaws

## Overview
Static code review of `CoreJsonSerializer.cs` identified several critical defects in deserialization parameter handling, options synchronization, object cloning, and JSON merge logic.

## Identified Defects

### 1. Dead `customConverters` Parameter in `Deserialize<T>`
In `CoreJsonSerializer.cs` (lines 327-330):
```csharp
public T Deserialize<T>(string json, params JsonConverter[] customConverters)
{
    return Deserialize<T>(json, _options);
}
```
The method signature advertises per-call custom converter support, but the implementation silently ignores `customConverters` and passes `_options`. Callers relying on per-call converters experience silent failure or fallback to default deserialization.

### 2. `Merge` Hardcodes `_mergeOptions.BaseOptions`
In `CoreJsonSerializer.cs` (lines 246-251):
```csharp
foreach (var value in values)
{
    if (value == null) continue;

    var jsonString = JsonSerializer.Serialize(value, _mergeOptions.BaseOptions);
    var jsonNode = JsonNode.Parse(jsonString);
...
```
The method receives `JsonMergeOptions mergeOptions` as an argument, but serializes incoming values using `_mergeOptions.BaseOptions` instead of `mergeOptions.BaseOptions` (or `mergeOptions?.BaseOptions ?? _mergeOptions.BaseOptions`). Any custom merge configuration passed by the caller is ignored during value serialization.

### 3. NullReferenceException on Null Elements in Array Union Merge
In `CoreJsonSerializer.cs` (lines 655-669):
```csharp
case JsonMergeArrayHandling.Union:
    var existingValues = new HashSet<string>();
    foreach (var item in target)
    {
        existingValues.Add(item.ToJsonString());
    }

    foreach (var item in source)
    {
        var itemJson = item.ToJsonString();
        if (!existingValues.Contains(itemJson))
        {
            target.Add(item?.DeepClone());
        }
    }
    break;
```
When `target` or `source` contains a `null` item, `item.ToJsonString()` throws `NullReferenceException`. Null items must be safely handled (e.g., using `"null"` or null-safe check).

### 4. `Options` Setter Desynchronization
In `CoreJsonSerializer.cs` (lines 27-37, 87-96):
`_nodeOptions` and `_docOptions` are computed only inside the constructor from `_options`. Setting `Options = newOptions` leaves `_nodeOptions` and `_docOptions` bound to the obsolete options instance.

### 5. `Clone()` Recreates Default Options Instead of Copying
In `CoreJsonSerializer.cs` (lines 99-104):
```csharp
public IJsonSerializer Clone()
{
    var options = CreateDefaultOptions();
    var mergeOptions = CreateDefaultMergeOptions();
    return new CoreJsonSerializer(options, mergeOptions);
}
```
`Clone()` does not copy the current serializer's `_options` and `_mergeOptions`. Instead, it generates fresh default options, discarding all custom settings, converters, and policies configured on the cloned instance.

## Remediation Plan
1. Implement proper per-call options cloning with `customConverters` appended in `Deserialize<T>`.
2. Update `MergeAndSerialize` to respect `mergeOptions?.BaseOptions ?? _mergeOptions.BaseOptions`.
3. Add null check in `JsonMergeArrayHandling.Union` before calling `ToJsonString()`.
4. Rebuild `_nodeOptions` and `_docOptions` in the `Options` property setter.
5. Fix `Clone()` to clone current `_options` and `_mergeOptions`.

