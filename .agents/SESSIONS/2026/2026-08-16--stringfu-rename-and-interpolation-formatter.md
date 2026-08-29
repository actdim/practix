---
date: 2026-08-16
slug: Emitron-rename-and-interpolation-formatter
agent: claude-sonnet-4-6-thinking
branch: main
commit: ~
summary: Renamed Emitron→Emitron, created Roslyn-based InterpolationFormatter with full test suite
---

## What changed & why

### Rename: Emitron → Emitron
- Renamed folder `ActDim.Emitron/` → `ActDim.Emitron/`
- Renamed csproj: `ActDim.Emitron.csproj` → `ActDim.Emitron.csproj`
- Updated namespace in `StringFormatter.cs`: `ActDim.Emitron` → `ActDim.Emitron`
- Updated `AssemblyTitle` in `Properties/AssemblyInfo.cs`
- Renamed folder `Tests/Emitron.Tests/` → `Tests/Emitron.Tests/`
- Renamed csproj: `ActDim.Emitron.Tests.csproj` → `ActDim.Emitron.Tests.csproj`
- Updated namespace in `StringFormatterTests.cs`
- Updated `<ProjectReference>` in tests csproj
- Updated solution file (all `Emitron` → `Emitron`)
- Added both projects to solution via `dotnet sln add` (they were previously missing)

### New: InterpolationFormatter
A Roslyn-scripting based formatter that compiles a C# interpolated string template
(`$"Hello, {Name}!"`) once and returns a cached `Func<object, string>` delegate.

**Design:**
- Template must be a valid C# interpolated string expression (starts with `$"` or `$@"`)
- A lightweight character-scanner rewrites `{Name}` → `{__p.Name}` inside the template
- The script globaltype `ScriptGlobals` carries an `ExpandoObject __vars`; the script body does `dynamic __p = __vars; return <rewritten-template>;`
- `ExpandoObject` is used instead of `Dictionary<string,object>` because only `IDynamicMetaObjectProvider` implementations support property-access via `dynamic`
- Parameters are supplied as any anonymous/POCO object or `IDictionary<string,object>`; public properties are reflected into the expando at each call
- Compilation is done once and cached by template text in a `ConcurrentDictionary`

**Files created:**
- `ActDim.Emitron/InterpolationFormatter.cs`
- `Tests/Emitron.Tests/InterpolationFormatterTests.cs`

**Packages added:**
- `Microsoft.CodeAnalysis.CSharp.Scripting` 5.6.0
- `Microsoft.CSharp` 4.7.0 (for dynamic binder support in script references)

## Test results
- All 14 new `InterpolationFormatterTests` pass
- All pre-existing 4 passing `StringFormatterTests` still pass
- 1 pre-existing failure: `StringFormatterTests.IsFastest` (broken before this session: benchmark logic compares to an empty loop)

## Decisions
- None new (ADR not required: this is a new standalone feature).

## Issues advanced
- None on board; feature delivered inline.

## Gaps / follow-ups
- `IsFastest` benchmark test is fundamentally broken; should be replaced with BenchmarkDotNet or removed.
- `ExpandoObject` property reflection happens on every call; consider caching the reflector per `parametersObj.GetType()` for hot-path usage.
- The `Microsoft.CSharp` NU1510 warning suggests the package may be redundant on .NET 10; investigate after Roslyn runtime assembly reference audit.
