---
protocol: along
slug: expose-blobmanager-and-corejsonserializer
date: 2026-09-24
agent: antigravity
summary: "Expose BlobManager, BlobManagerBuilder, and CoreJsonSerializer as Public"
---

# Session: Expose BlobManager, BlobManagerBuilder, and CoreJsonSerializer as Public
Date: 2026-09-24
Issue: `feat--expose-blobmanager-and-corejsonserializer`

## Objectives
Expose internal implementation classes as public types to enable direct consumption without requiring Microsoft Dependency Injection:
1. `ActDim.BytePath.BlobManager`: changed from `internal class` to `public class` with XML documentation on the class and its constructors.
2. `ActDim.BytePath.Extensions.BlobManagerBuilder`: changed from `internal sealed class` to `public sealed class` with XML documentation on the class, constructor, and properties.
3. `ActDim.Practix.Json.CoreJsonSerializer`: changed from `internal class` to `public class` with XML documentation on the class and its constructors.
4. Cleaned legacy box drawing characters and non-ASCII ellipses across `CoreJsonSerializer.cs`.

## Changes Made
- BlobManager.cs: declared `public class BlobManager : IBlobManager`, added authoritative XML docs for class and constructors.
- BlobManagerBuilder.cs: declared `public sealed class BlobManagerBuilder : IBlobManagerBuilder`, added authoritative XML docs for class, constructor, and properties.
- CoreJsonSerializer.cs: declared `public class CoreJsonSerializer`, added XML docs for class and constructors, converted box drawing section dividers and unicode ellipsis to clean ASCII.

## Verification
- Executed `dotnet test ActDim.Practix.sln -v q` across the solution:
  - 12 test projects executed.
  - 732 tests passed, 0 failures, 0 skipped.
- Verified typography across all modified files (100% clean ASCII).
