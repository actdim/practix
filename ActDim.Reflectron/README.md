# ActDim.Reflectron

`ActDim.Reflectron` is a high-performance, memory-safe .NET reflection and dynamic member access engine. It provides compiled expression-tree property/field accessors, cached delegates, and fluent weak-referenced object wrappers that eliminate traditional `System.Reflection` runtime overhead.

## Features

- **Fast Indexer & Member Access:** Read and write properties or fields by string name (`reflector["Prop"] = value`) or lambda expression with near-native execution speed.
- **Memory-Safe Weak References:** Instance reflectors hold the target object via `WeakReference<T>`, allowing the garbage collector to reclaim unused objects without memory leaks.
- **Compiled Expression-Tree Caching:** Automatic, concurrent, thread-safe caching of compiled getters, setters, constructors, and method invocators.
- **Strongly-Typed & Dynamic Type Factories:** Obtain reusable reflector factories for static types (`typeof(User).Reflect<User>()`) or runtime types (`type.Reflect()`).
- **Fast Dynamic Method & Constructor Invocations:** Generate high-speed invokers (`FastMethodCallDelegate`, `FastDynamicDelegate`) and DynamicMethod IL constructors.
- **Safe Member Discovery:** Retrieve `MemberInfo`, `PropertyInfo`, or `MethodInfo` safely through strongly-typed lambda expressions (`Reflectron.GetMemberInfo((User u) => u.Name)`).

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Reflectron
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Reflectron
```

---

## Quick Start & Usage Examples

### 1. Fluent Object Reflector (`obj.Reflect()`)

Call `.Reflect()` on an object instance **once** to obtain a reusable, memory-safe `IReflectron<T>` wrapper:

```csharp
using ActDim.Reflectron;

var user = new User { Name = "Initial", Age = 25 };

// Obtain the reflector once for the target instance:
var reflector = user.Reflect();

// Read and write via indexer (supports properties and fields):
reflector["Name"] = "Alice";
reflector["Age"] = 30;
Console.WriteLine(reflector["Name"]); // Output: Alice

// Strongly-typed reading and writing by lambda expression:
string updatedName = reflector.Set(u => u.Name, "Bob");
int age = reflector.Get(u => u.Age);

// Reading and writing by member name:
reflector.Set("Name", "Charlie");
string name = reflector.Get<string>("Name");
```

### 2. Calling Methods via Reflector

Extract cached method delegates by name or expression from the instance reflector:

```csharp
using ActDim.Reflectron;

var user = new User { Name = "Alice" };
var reflector = user.Reflect();

// By method name:
var greetByName = reflector.GetMethod<Func<User, string, string>>("FormatGreeting");
string result1 = greetByName(user, "Hello"); // Output: Hello, Alice!

// By lambda expression:
var greetByExpr = reflector.GetMethod<Func<User, string, string>, string>(u => u.FormatGreeting(default));
string result2 = greetByExpr(user, "Welcome");
```

### 3. Cached Type Reflector Factories

Obtain reusable factory delegates to spawn reflectors efficiently:

```csharp
using ActDim.Reflectron;

// 1. Strongly-typed factory for known compile-time types:
Func<User, IReflectron<User>> userReflectorFactory = typeof(User).Reflect<User>();

foreach (var u in userList)
{
    var r = userReflectorFactory(u);
    r.Set(x => x.Age, 35);
}

// 2. Runtime Type factory for dynamically resolved types:
Type runtimeType = payload.GetType();
Func<object, IReflectron<object>> dynamicFactory = runtimeType.Reflect();

var dynamicReflector = dynamicFactory(payload);
dynamicReflector["Status"] = "Processed";
```

### 4. Direct High-Performance Delegate Caches

When writing performance-critical frameworks, use static `Reflectron` caches directly:

```csharp
using ActDim.Reflectron;
using System.Reflection;

// Property getters and setters:
PropertyInfo propInfo = typeof(User).GetProperty(nameof(User.Name));
Func<User, string> nameGetter = Reflectron.GetPropertyGetter<User, string>(propInfo);
Action<User, string> nameSetter = Reflectron.GetPropertySetter<User, string>(propInfo);

nameSetter(user, "David");
string currentName = nameGetter(user);

// Dynamic method invocation:
MethodInfo method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
FastMethodCallDelegate fastCaller = Reflectron.GetMethodCaller(method);
object upper = fastCaller("hello world", null); // Output: HELLO WORLD

// High-speed compiled constructor:
Func<User> userCtor = Reflectron.CreateConstructor<Func<User>>();
User newUser = userCtor();
```

### 5. Memory Safety & Weak References

`Reflectron<T>` uses `WeakReference<T>` internally. Long-lived reflectors will never cause memory leaks by holding dead instances:

```csharp
IReflectron<User> reflector;

void Initialize()
{
    var tempUser = new User { Name = "ShortLived" };
    reflector = tempUser.Reflect();
    Console.WriteLine(reflector["Name"]); // Output: ShortLived
}

Initialize();

GC.Collect();
GC.WaitForPendingFinalizers();

// Target object was collected; attempting access throws ReflectionException:
// reflector["Name"] -> throws ReflectionException("Can't access target object")
```

---

## Testing & Quality

- **Test Suite:** `ActDim.Reflectron.Tests`
- **Total Tests:** 56 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Reflectron.Tests/ActDim.Reflectron.Tests.csproj
```

---

## License

This project is licensed under the [MIT License](LICENSE).
