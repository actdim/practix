---
protocol: along
protocol_version: "2.2.18"
slug: setup-and-workflow
title: Setup & Developer Workflow
type: setup-workflow
created: 2026-09-03
updated: 2026-09-03
tags: [setup, workflow, testing, nuget, razor]
---

# Setup & Developer Workflow

Package installation and automated testing for `ActDim.Emitron.Razor`.

---

## Installation

```bash
dotnet add package ActDim.Emitron.Razor
```

---

## Test Execution

The test suite is located in `Tests/Emitron.Razor.Tests/`:

```bash
dotnet test Tests/Emitron.Razor.Tests/ActDim.Emitron.Razor.Tests.csproj -v q
```

### Verified Test Suites (8 Tests Total):
- `RazorParserTests`: Model property substitution, conditional blocks (`@if / @else`), iteration loops (`@foreach`), embedded C# expressions.
