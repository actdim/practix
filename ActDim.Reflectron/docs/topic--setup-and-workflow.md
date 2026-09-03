---
protocol: along
protocol_version: "2.2.18"
slug: setup-and-workflow
title: Setup & Developer Workflow
type: setup-workflow
created: 2026-09-03
updated: 2026-09-03
tags: [setup, workflow, testing, nuget, benchmarks]
---

# Setup & Developer Workflow

Package installation, developer workflow, and test execution for `ActDim.Reflectron`.

---

## Installation

```bash
dotnet add package ActDim.Reflectron
```

---

## Test Execution

The test suite is located in `Tests/Reflectron.Tests/`:

```bash
dotnet test Tests/Reflectron.Tests/ActDim.Reflectron.Tests.csproj -v q
```

### Verified Test Suites (56 Tests Total):
- `ReflectronPropertyTests`: Property getter and setter expressions, indexer lookups, private properties.
- `ReflectronFieldTests`: Field getter and setter expressions.
- `ReflectronConstructorTests`: Default and parameterized compiled constructor invocations.
- `ReflectronMethodTests`: Fast dynamic method calls and parameter passing.
- `ReflectronWeakReferenceTests`: Target GC reclamation and exception behavior.
