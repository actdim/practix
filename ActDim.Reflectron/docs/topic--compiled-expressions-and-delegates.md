---
protocol: along
protocol_version: "2.2.26"
slug: compiled-expressions-and-delegates
title: Compiled Expressions and Delegate Caching
type: topic
curated: true
created: 2026-09-03
updated: 2026-09-08
tags: [reflection, expressions, delegates, caching, dynamic-method, constructors]
---

# Compiled Expressions and Delegate Caching

`ActDim.Reflectron` bypasses the performance penalties of standard `System.Reflection` (boxing, unboxing, runtime dispatch, metadata lookups) by generating and caching strongly-typed delegates compiled from LINQ Expression Trees and DynamicMethod IL.

---

## High-Performance Property and Field Accessors

Static cache methods in `Reflectron` compile runtime `PropertyInfo` and `FieldInfo` into strongly-typed native delegates:

```csharp
using ActDim.Reflectron;
using System.Reflection;

PropertyInfo propInfo = typeof(User).GetProperty(nameof(User.Name));

// Strongly-typed compiled delegates
Func<User, string> getter = Reflectron.GetPropertyGetter<User, string>(propInfo);
Action<User, string> setter = Reflectron.GetPropertySetter<User, string>(propInfo);

setter(user, "Alice");
string name = getter(user);
```

### Supported Member Types
- **Properties**: Instance, static, indexed, and auto-properties via `Reflectron.Properties.cs`.
- **Fields**: Public and non-public fields via `Reflectron.Fields.cs`.
- **Untyped Overloads**: Object-based accessors for dynamic scenarios where member types are resolved only at runtime.

---

## Dynamic Method Invocation

For method invocation without `MethodInfo.Invoke` overhead, `Reflectron` emits `FastMethodCallDelegate`:

```csharp
using ActDim.Reflectron;
using System.Reflection;

MethodInfo method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
FastMethodCallDelegate caller = Reflectron.GetMethodCaller(method);

object result = caller("hello world", null); // Output: HELLO WORLD
```

`FastMethodCallDelegate` signatures accept `(object target, object[] args)` and invoke the underlying method through compiled expression casts, eliminating reflection lookup loops.

---

## Compiled Constructor Generation

Instead of `Activator.CreateInstance`, `Reflectron` generates delegates targeting constructors:

```csharp
using ActDim.Reflectron;

// Parameterless constructor delegate
Func<User> ctor = Reflectron.CreateConstructor<Func<User>>();
User instance = ctor();

// Parameterized constructor delegate
Func<string, int, User> parameterizedCtor = Reflectron.CreateConstructor<Func<string, int, User>>();
User userWithData = parameterizedCtor("Alice", 30);
```

---

## Thread-Safe Caching Strategy

- Delegates are compiled once on initial access and cached in thread-safe concurrent collections.
- Cache keys combine the target type, member metadata token, and delegate signature to prevent collisions across generic types and interface implementations.
- Successive calls resolve from the cache in sub-microsecond time with zero allocation.

---

## Related Topics

- [System Architecture & Flow](./topic--architecture.md)
- [Dynamic Member Access & Lifetime Management](./topic--dynamic-member-access.md)
- [Knowledge Base Index](./INDEX.md)
