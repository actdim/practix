---
protocol: along
slug: stj-converters-and-newtonsoft-incompatibilities
type: debt
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [json, converters, newtonsoft-compat, security, xss, reflection]
milestone: v2.0.0-along-transition
blocked_by: []
related: [core-json-serializer-contract-and-merge-flaws]
---

# STJ Converters Discrepancies and Inaccurate Newtonsoft Compatibility

## Overview
Default options in `CoreJsonSerializer` claim "Newtonsoft JSON compatibility", but several configured settings break compatibility or introduce subtle security and runtime issues. In addition, several custom converters suffer from reflection performance bottlenecks, asymmetric behavior, or orphaned code.

## Identified Defects

### 1. Incompatible and Risky Serializer Defaults
In `CoreJsonSerializer.cs` (lines 121-125):
- `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow`: Throws an exception when encountering extra unmapped JSON members during deserialization. In Newtonsoft.Json, unmapped members are ignored by default (`MissingMemberHandling.Ignore`).
- `ReferenceHandler = ReferenceHandler.IgnoreCycles`: Silently drops cyclic references by setting them to null or skipping them, which can result in unexpected data loss compared to explicit reference tracking.
- `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping`: Disables strict HTML character escaping (`<`, `>`, `&`). If serialized JSON is directly embedded into HTML contexts, this introduces XSS vulnerabilities.

### 2. Orphaned `ObjectToInferredTypesConverter`
`ObjectToInferredTypesConverter.cs` provides token-to-primitive parsing into `JsonElement`, while `ObjectJsonConverter.cs` parses into `ExpandoObject` and `List<object>` and is registered in `CreateDefaultOptions()`. `ObjectToInferredTypesConverter` is not referenced in the default serializer pipeline and acts as redundant code.

### 3. Reflection Inefficiencies and Operator Mixing in `ImplicitOperatorConverter`
In `ImplicitOperatorConverterFactory.cs`:
- Every `Write` call invokes `_implicitToPrimitive.Invoke(null, [value])` via uncompiled reflection.
- Every `Read` call invokes `_implicitFromPrimitive.Invoke(null, [primitive])` via uncompiled reflection.
- Searches for both `op_Implicit` and `op_Explicit` interchangeably, despite the type name advertising implicit conversions.
- `TargetPrimitives` hardcodes a fixed array order; the first matched primitive is picked regardless of appropriateness or precision.

### 4. `ExceptionJsonConverter.Read` Unconditionally Throws
In `ExceptionJsonConverter.cs` (lines 20-23):
```csharp
public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    throw new NotSupportedException("Deserializing exceptions is not supported.");
}
```
Any DTO or message envelope that contains a property of type `Exception` (e.g. error responses, task results) cannot be deserialized, throwing `NotSupportedException`.

## Remediation Plan
1. Re-evaluate `UnmappedMemberHandling` default (switch to `Skip` for true Newtonsoft parity) and document `UnsafeRelaxedJsonEscaping` security implications.
2. Consolidate or deprecate `ObjectToInferredTypesConverter` in favor of `ObjectJsonConverter`.
3. Compile expression lambdas (`Func<TSource, TPrimitive>` / `Func<TPrimitive, TSource>`) in `ImplicitOperatorConverter` instead of using `MethodInfo.Invoke`. Separate implicit and explicit operator handling.
4. Support deserialization in `ExceptionJsonConverter` into a generic `Exception` or custom `SerializedExceptionInfo` DTO rather than throwing `NotSupportedException`.

