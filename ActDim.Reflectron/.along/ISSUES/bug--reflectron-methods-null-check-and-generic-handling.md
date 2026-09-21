---
protocol: along
slug: reflectron-methods-null-check-and-generic-handling
type: bug
status: open
priority: high
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [reflection, methods, null-reference, generic-methods, overloads]
milestone: v2.0.0-along-transition
blocked_by: []
related: [reflectron-property-getter-setter-cache-asymmetry]
---

# Reflectron.Methods Null Checks, Generic Overloads, and Missing Method Handling

## Overview
`ActDim.Reflectron.Reflectron.Methods.cs` resolves methods via `type.GetMethod(...)`. If the method does not exist or parameter types do not match, `type.GetMethod` returns null. The code immediately passes this null reference into `GetMethodCaller<TDelegate>(methodInfo)` without validation, causing an unhelpful `ArgumentNullException` or `NullReferenceException`. In addition, there is no support for generic methods or flexible overload resolution.

## Identified Defects

### 1. Null `MethodInfo` Passed Without Check
In `Reflectron.Methods.cs` (lines 92-99):
```csharp
var methodInfo = type.GetMethod(
    name,
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
    null,
    invokeParamTypes,
    new ParameterModifier[0]);

return GetMethodCaller<TDelegate>(methodInfo);
```
And similarly for instance methods (lines 139-146):
If `type.GetMethod(...)` fails to find a matching method, `methodInfo` is `null`. `GetMethodCaller<TDelegate>(methodInfo)` calls `Guard.Against.Null(method, nameof(method))`, throwing `ArgumentNullException` with no indication of what type, method name, or signature failed to resolve.

### 2. Missing Generic Method Support and Rigid Overload Matching
Method lookup strictly matches `invokeParamTypes` extracted from the delegate's `Invoke` signature. It does not:
- Resolve open generic methods and construct closed generic methods matching delegate type arguments.
- Handle method overloads that require covariant/contravariant parameter conversion or optional parameters.
- Provide a descriptive exception (such as `MissingMethodException`) detailing candidate overloads when lookup fails.

## Remediation Plan
1. Check `if (methodInfo == null)` and throw a descriptive `MissingMethodException` containing `type.FullName`, `name`, and parameter types.
2. Extend `Reflectron.Methods` to support generic method definition resolution and closed generic instantiation when requested.
3. Enhance overload resolution to match assignable/convertible parameter signatures.

