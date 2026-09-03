---
protocol: along
protocol_version: "2.2.18"
slug: domain-model
title: Domain Model & Entities
type: domain-model
created: 2026-09-03
updated: 2026-09-03
tags: [domain-model, interfaces, reflection, delegates, exceptions]
---

# Domain Model & Entities

Core abstractions, delegate types, interfaces, and exception specifications across `ActDim.Reflectron`.

---

## Core Interfaces & Abstractions

| Type | Kind | Description |
| :--- | :--- | :--- |
| `IReflectron<T>` | Interface | Generic instance reflector wrapper over a target object of type `T`. |
| `IReflectron` | Interface | Non-generic instance reflector for dynamically typed objects. |
| `Reflectron` | Static Class | Central factory, cache manager, and expression compiler facade. |
| `FastMethodCallDelegate` | Delegate | High-performance method caller delegate `(object instance, object[] args) => object`. |
| `FastDynamicDelegate` | Delegate | Dynamic invoker delegate for variable parameter signatures. |
| `ReflectionException` | Exception | Thrown when target member is missing or dead weak-reference is accessed. |

---

## Error Handling & Exception Guarantees

1. **Dead Target Access**: Accessing any member on an `IReflectron<T>` whose underlying target has been reclaimed by the Garbage Collector throws `ReflectionException("Target object is no longer alive.")`.
2. **Missing Member Access**: Attempting to get or set a non-existent property or field by name throws `ReflectionException("Member not found.")`.
3. **Type Mismatch**: Attempting to set an incompatible value type to a strongly-typed member throws `ArgumentException` or `InvalidCastException`.
