# C# / .NET Coding Standards & Best Practices

Modern .NET 8+ and C# 12 engineering conventions based on Microsoft Framework Design Guidelines, Central Package Management, and high-performance patterns.

---

## 1. Central Package Management (CPM) & Solution Architecture

- **Centralized Dependency Management**:
  - Always enable Central Package Management (CPM) in solutions by placing a `Directory.Packages.props` file in the repository root with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
  - Define all NuGet package versions centrally in `Directory.Packages.props`. Individual `*.csproj` files must specify `<PackageReference Include="Package.Name" />` without `Version="..."` attributes.
  - If a specific project requires an isolated version override, explicitly use `<PackageReference Include="Package.Name" VersionOverride="x.y.z" />`.

- **Shared Build Properties (`Directory.Build.props`)**:
  - Place a root `Directory.Build.props` to enforce uniform compiler settings across all projects (`<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<TargetFramework>net8.0</TargetFramework>`, `<ImplicitUsings>enable</ImplicitUsings>`).

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageVersion Include="MediatR" Version="12.4.0" />
    <PackageVersion Include="xunit" Version="2.9.0" />
  </ItemGroup>
</Project>
```

---

## 2. Type Safety & Nullability

- **Nullable Reference Types**: Always enable `<Nullable>enable</Nullable>`.
- **Zero Null Suppressions**: Avoid `!` (null-forgiving operator) unless mathematically guaranteed and documented. Use null-coalescing (`??`, `??=`) and null-conditional (`?.`) operators.
- **Pattern Matching**: Prefer pattern matching (`is`, `switch` expressions) over explicit casting (`as`, `(Type)`).

```csharp
// Recommended: Pattern matching with property extraction
if (user is { IsActive: true, Email: { Length: > 0 } email })
{
    await SendNotificationAsync(email, cancellationToken);
}
```

---

## 3. Immutability & Data Modeling

- **Records**: Use `record` or `record struct` for DTOs, events, and value objects.
- **Readonly Structs**: Use `readonly struct` for small, high-throughput value types to eliminate defensive copying.
- **Primary Constructors**: Use C# 12 primary constructors on classes and records for clean dependency injection.

```csharp
// Recommended: Primary constructor dependency injection
public sealed class OrderService(
    IOrderRepository repository,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResult> ProcessAsync(OrderId id, CancellationToken ct = default)
    {
        // ...
    }
}
```

---

## 4. Asynchronous Programming Guidelines

- **Always Pass `CancellationToken`**: Every async method accepting external I/O must accept and propagate a `CancellationToken`.
- **No `async void`**: Use `async void` strictly for event handlers; all other async methods must return `Task` or `ValueTask`.
- **ValueTask for Hot Paths**: Return `ValueTask<T>` for high-frequency methods that frequently complete synchronously (e.g. cache lookups).
- **Avoid `.Result` and `.Wait()`**: Never block on asynchronous code (prevents thread-pool starvation and deadlocks).

---

## 5. Naming & Formatting Conventions

- **PascalCase**: Classes, Records, Structs, Enums, Interfaces, Methods, Properties, Public fields.
- **camelCase**: Local variables, method arguments, private fields with `_` prefix (e.g. `_orderRepository`).
- **Interfaces**: Always prefix with `I` (e.g. `IUserService`).
- **Async Suffix**: Always append `Async` to asynchronous methods (e.g. `FetchUserDataAsync`).
- **File-scoped Namespaces**: Use file-scoped namespaces (`namespace MyProject.Services;`) to reduce indentation.

---

## 6. NuGet Packaging & AI Documentation (LLM-Wiki)

- **Centralized Packaging via `Directory.Build.props` (Recommended)**:
  - Place AI context packaging rules in the solution root `Directory.Build.props` to ensure every published library automatically includes its agent guidelines and project wiki.
  - Enable XML documentation generation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
  - Pack `README.md`, `AGENTS.md`, `llms.txt`, and the full `docs/` Knowledge Base into the generated `.nupkg`.

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageReadmeFile Condition="Exists('README.md')">README.md</PackageReadmeFile>
    <PackageTags>along;ai-agent;llms;$(PackageTags)</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <!-- Package Readme -->
    <None Include="README.md" Pack="true" PackagePath="" Condition="Exists('README.md')" />

    <!-- AI Instructions in Package Root -->
    <None Include="AGENTS.md" Pack="true" PackagePath="" Condition="Exists('AGENTS.md')" />
    <None Include="llms.txt" Pack="true" PackagePath="" Condition="Exists('llms.txt')" />

    <!-- Project Knowledge Base (Wiki) in docs/ folder -->
    <None Include="docs\**\*" Pack="true" PackagePath="docs" Condition="Exists('docs')" />
  </ItemGroup>
</Project>
```

- **Standalone Project Configuration (`*.csproj`)**:
  - For independent libraries or repositories without `Directory.Build.props`, declare packaging metadata explicitly in the project file:

```xml
<!-- MyLibrary.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>MyOrg.MyLibrary</PackageId>
    <Version>1.0.0</Version>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>along;ai-agent;llms;$(PackageTags)</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="" />
    <None Include="AGENTS.md" Pack="true" PackagePath="" />
    <None Include="llms.txt" Pack="true" PackagePath="" />
    <None Include="docs\**\*" Pack="true" PackagePath="docs" />
  </ItemGroup>
</Project>
```

- **Consumer & Upward Discovery Protocol**:
  - When consumers install the package via `dotnet add package` or `<PackageReference>`, NuGet unpacks the package into the global cache (`~/.nuget/packages/<package_id>/<version>/`).
  - Along dependency scanner (`along-dep-scan`) inspects `<PackageReference>` elements, finds the unpacked package in the NuGet cache, indexes `AGENTS.md` and `docs/`, and links them in the consumer's `docs/topic--dependencies.md`.


