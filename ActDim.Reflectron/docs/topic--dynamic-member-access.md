---
protocol: along
protocol_version: "2.2.26"
slug: dynamic-member-access
title: Dynamic Member Access and Lifetime Management
type: topic
curated: true
created: 2026-09-03
updated: 2026-09-08
tags: [reflection, dynamic-access, indexer, weak-reference, memory-safety, factories]
---

# Dynamic Member Access and Lifetime Management

`ActDim.Reflectron` provides fluent, memory-safe member access and mutation through `IReflectron<T>`, eliminating boilerplate while protecting long-lived caches from memory leaks.

---

## Fluent Instance Reflector (`.Reflect()`)

Calling `.Reflect()` on any reference object wraps the instance in `IReflectron<T>`:

```csharp
using ActDim.Reflectron;

var user = new User { Name = "Initial", Age = 25 };
var reflector = user.Reflect();

// Indexer access for properties and fields
reflector["Name"] = "Alice";
reflector["Age"] = 30;
Console.WriteLine(reflector["Name"]); // Output: Alice

// Strongly-typed access via lambda expressions
reflector.Set(u => u.Name, "Bob");
int age = reflector.Get(u => u.Age);

// String-based typed access
reflector.Set("Name", "Charlie");
string name = reflector.Get<string>("Name");
```

---

## Memory Safety via `WeakReference<T>`

Long-lived caches or wrappers can inadvertently prevent garbage collection if they hold strong references to transient objects.

`Reflectron<T>` holds its target instance through `WeakReference<T>`. When the target object is no longer referenced elsewhere in the application, the .NET Garbage Collector reclaims it normally:

```csharp
using ActDim.Reflectron;

IReflectron<User> reflector;

void Scope()
{
    var transient = new User { Name = "Temporary" };
    reflector = transient.Reflect();
}

Scope();
GC.Collect();
GC.WaitForPendingFinalizers();

// Accessing the target after collection throws ReflectionException
// reflector["Name"] -> throws ReflectionException("Can't access target object")
```

If the target object has been collected, subsequent calls to `Get`, `Set`, or indexers throw `ReflectionException`.

---

## Type Reflector Factories

When processing collections of objects, creating individual reflector delegates per item introduces unnecessary allocation. Use cached reflector factories instead:

### Strongly-Typed Compile-Time Factory

```csharp
using ActDim.Reflectron;

Func<User, IReflectron<User>> userFactory = typeof(User).Reflect<User>();

foreach (var user in users)
{
    var r = userFactory(user);
    r.Set(u => u.Status, "Active");
}
```

### Dynamic Runtime Type Factory

```csharp
using ActDim.Reflectron;

Type runtimeType = payload.GetType();
Func<object, IReflectron<object>> dynamicFactory = runtimeType.Reflect();

var r = dynamicFactory(payload);
r["ProcessedAt"] = DateTime.UtcNow;
```

---

## Related Topics

- [System Architecture & Flow](./topic--architecture.md)
- [Compiled Expressions and Delegate Caching](./topic--compiled-expressions-and-delegates.md)
- [Knowledge Base Index](./INDEX.md)
