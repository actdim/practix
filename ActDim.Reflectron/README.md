# ActDim.Reflectron

`ActDim.Reflectron` is a high-performance .NET reflection engine providing compiled expression-tree property getters, setters, dynamic method callers, and strongly-typed member accessors that eliminate traditional `System.Reflection` runtime invocation overhead.

## Features

- **Compiled Expression Tree Accessors:** Compile property getters and setters into `Func<object, object>` and `Action<object, object>` delegates cached per property.
- **Fast Dynamic Method Invocation:** Call methods dynamically using compiled delegates (`FastMethodCallDelegate`, `FastDynamicDelegate`) rather than expensive `MethodInfo.Invoke`.
- **Expression-Based Member Access:** Safely retrieve `MemberInfo`, `PropertyInfo`, or `MethodInfo` using strongly-typed lambda expressions (`TypeAccess.GetMemberInfo(() => obj.Property)`) instead of dangerous magic strings.
- **Generic Type Factory Helpers:** Helper utilities for dynamically creating `Func` and `Action` generic delegates (`TypeAccess.GetFuncType`, `TypeAccess.GetActionType`).
- **Zero Configuration Caching:** Built-in concurrent thread-safe delegate caches for optimal repeated performance.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Reflectron
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Reflectron
```

## Quick Start Examples

### 1. Strongly-Typed Member Retrieval

```csharp
using ActDim.Reflectron;
using System.Reflection;

public class User
{
    public string Name { get; set; }
}

// Retrieve PropertyInfo safely without string literals
MemberInfo member = TypeAccess.GetMemberInfo((User u) => u.Name);
Console.WriteLine(member.Name); // Output: Name
```

### 2. Fast Property Setters and Getters

```csharp
using ActDim.Reflectron;
using System.Reflection;

var user = new User { Name = "Initial" };
PropertyInfo propInfo = typeof(User).GetProperty(nameof(User.Name));

// Obtain or create cached compiled setter
var setter = TypeAccess.GetPropertySetter(propInfo);

// Execute compiled setter (near-native speed)
setter(user, "Updated Name");

Console.WriteLine(user.Name); // Output: Updated Name
```

### 3. Fast Dynamic Delegate Calls

```csharp
using ActDim.Reflectron;
using System.Reflection;

MethodInfo method = typeof(string).GetMethod(nameof(string.ToUpper), System.Type.EmptyTypes);
var fastCaller = FastMethodCallDelegate.Create(method);

object result = fastCaller("hello world", null);
Console.WriteLine(result); // Output: HELLO WORLD
```

## License

This project is licensed under the [MIT License](LICENSE).
