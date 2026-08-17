---
date: 2026-08-17
slug: move-incomplete-data-exception-to-abstractions
agent: antigravity
branch: main
commit: head
summary: Created DataFormatException in ActDim.Practix.Abstractions.Exceptions, derived IncompleteDataException from DataFormatException, and standardized framework exception namespaces under ActDim.Practix.Abstractions.Exceptions.
---

# Session Log: Relocate Exception Classes & Standardize Namespaces

## Changes Made & Rationale
- **Standardized Namespace Convention in `Abstractions`**:
  - Aligned all exception types in `ActDim.Practix.Abstractions` under `namespace ActDim.Practix.Abstractions.Exceptions` (matching the convention of `.Serialization`, `.Compression`, `.IO`, `.Context`, etc.).
- **Introduced `DataFormatException`**:
  - Created `ActDim.Practix.Abstractions/Exceptions/DataFormatException.cs` in `ActDim.Practix.Abstractions.Exceptions`.
  - Represents domain errors where data payload, header, protocol, or buffer fails format/validation checks without string-parsing or stream-IO coupling.
- **`IncompleteDataException` Relocation & Subclassing**:
  - Moved `IncompleteDataException` to `ActDim.Practix.Abstractions/Exceptions/IncompleteDataException.cs`.
  - Derived `IncompleteDataException` from `DataFormatException` (`public class IncompleteDataException : DataFormatException`).
- **Eliminated Custom `InvalidDataException`**:
  - Deleted `ActDim.Practix.Common/InvalidDataException.cs` to remove type shadowing against `System.IO.InvalidDataException`.
  - Replaced `System.IO.InvalidDataException` in `CompressionManager` and `CompressionManagerTests` with `DataFormatException`.
  - Updated `EntityRefExtensions.cs` to throw `DataFormatException`.
- **Recorded Architectural Decision**:
  - Recorded ADR-013 in `.agents/DECISIONS.md`.

## Files Touched
- `ActDim.Practix.Abstractions/Exceptions/DataFormatException.cs` [NEW]
- `ActDim.Practix.Abstractions/Exceptions/IncompleteDataException.cs` [NEW]
- `ActDim.Practix.Common/InvalidDataException.cs` [DELETED]
- `ActDim.Practix.Common/IncompleteDataException.cs` [DELETED]
- `ActDim.Practix.Abstractions/Compression/ICompressionManager.cs`
- `ActDim.Practix.Common/Compression/CompressionManager.cs`
- `Tests/Common.Tests/Compression/CompressionManagerTests.cs`
- `AppRegistry/AppRegistry.Domain/Core/EntityRefExtensions.cs`
- `.agents/DECISIONS.md`

## Verification
- Executed `dotnet test ActDim.Practix.sln` — all 493 tests passed across all 6 test assemblies.
