---
protocol: along
slug: reflectron-property-getter-setter-cache-asymmetry
type: bug
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [reflection, properties, getters, setters, casting, asymmetry, pkg-reflectron]
milestone: v2.0.0-along-transition
blocked_by: []
related: [reflectron-methods-null-check-and-generic-handling]
---

# Property Getter vs Setter Cache Asymmetry and InvalidCastException

## Overview
Analysis of property getter and setter resolution in `ActDim.Reflectron.Reflectron.Properties.cs` identified cache type collisions resulting in `InvalidCastException`, asymmetry in how getters vs setters are cached, and inconsistent accessibility flags (`nonPublic`) between getters, setters, and properties.

## Identified Defects

### 1. Named Getters Cast Untyped Cache Resulting in `InvalidCastException`
In `Reflectron.Properties.cs` (lines 51-70):
```csharp
public static Delegate GetPropertyGetter(Type type, string name)
{
    ...
    var propInfo = type.GetProperty(name);
    return GetPropertyGetter(propInfo);
}

public static Func<T, TProperty> GetPropertyGetter<T, TProperty>(string name)
{
    return (Func<T, TProperty>)GetPropertyGetter(typeof(T), name);
}
```
`GetPropertyGetter(propInfo)` uses the untyped key `(typeof(Delegate), propInfo)` in `TypedPropertyGetterCache`.
In `GetPropertyGetter((Type, PropertyInfo) pair)` (lines 165-167):
When the key has `typeof(Delegate)`, it creates a delegate typed with the concrete declaring type: `Func<ConcreteType, PropertyType>`.
If caller then invokes `TypeExtensions.GetPropertyGetter<Func<object, TProperty>>(propInfo)` or requests a getter with a base class/interface as instance type, the cached delegate is of type `Func<ConcreteType, ...>`. Direct casting to `Func<object, ...>` throws `InvalidCastException`.

### 2. Cache Key Asymmetry: Getters Untyped vs Setters Typed
In `Reflectron.Properties.cs` (lines 136-142):
```csharp
public static Action<T, TProperty> GetPropertySetter<T, TProperty>(string name)
{
    ...
    return GetPropertySetter<T, TProperty>(propInfo);
}
```
`GetPropertySetter<T, TProperty>` looks up the typed cache key `(typeof(Action<T, TProperty>), propInfo)`.
In contrast, `GetPropertyGetter<T, TProperty>(string name)` routes through the untyped `(typeof(Delegate), propInfo)` key. This architectural asymmetry leads to subtle cache poisoning and casting bugs.

### 3. Asymmetric Non-Public Accessibility and Public-Only `type.GetProperty`
- In line 173: `var getMethod = propInfo.GetGetMethod();` (omits `nonPublic: true`, so private getters cannot be read).
- In line 229: `var setMethod = propInfo.GetSetMethod(true);` (passes `nonPublic: true`, so private setters can be written).
- In lines 56, 124, 139: `type.GetProperty(name)` uses default `BindingFlags`, searching only public instance properties, rendering internal and private properties unreachable by name.

## Remediation Plan
1. Unify `GetPropertyGetter<T, TProperty>` to look up by explicit typed key `(typeof(Func<T, TProperty>), propInfo)`, matching `GetPropertySetter`.
2. Provide consistent `BindingFlags` for member lookup (e.g. `Public | NonPublic | Instance`).
3. Harmonize getter and setter visibility handling (`GetGetMethod(true)` matching `GetSetMethod(true)`).

