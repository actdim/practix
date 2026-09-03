---
protocol: along
protocol_version: "2.2.18"
slug: setup-and-workflow
title: Setup, Configuration & Developer Workflow
type: setup-workflow
created: 2026-09-03
updated: 2026-09-03
tags: [setup, workflow, testing, nuget, roslyn]
---

# Setup, Configuration & Developer Workflow

Installation, configuration options, and automated testing instructions for `ActDim.Emitron`.

---

## Installation

```bash
dotnet add package ActDim.Emitron
```

---

## Global Options Configuration

Configure default assembly search paths or global usings application-wide:

```csharp
using ActDim.Emitron;

Emitron.DefaultOptions
    .AddSearchPaths(@"C:\Plugins\Assemblies")
    .AddUsings("System.Text.Json", "System.Collections.Generic");
```

---

## Test Execution

The test suite is located in `Tests/Emitron.Tests/`:

```bash
dotnet test Tests/Emitron.Tests/ActDim.Emitron.Tests.csproj -v q
```

### Verified Test Suites (54 Tests Total):
- `EmitronTemplateTests`: Direct variable binding, format strings, ternary conditionals, method invocations.
- `EmitronScriptTests`: Multi-statement logic, mathematical algorithms, boolean predicates.
- `EmitronDirectiveTests`: `#r` assembly references and `using` namespace resolution.
- `EmitronOptionsTests`: Custom search paths, type registration, parameter variable renaming.
