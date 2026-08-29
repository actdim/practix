---
protocol: along
date: 2026-08-25
slug: extract-actdim-three-newtonsoftjson
agent: gemini-3.6-flash
branch: main
commit: HEAD
summary: Extracted Newtonsoft.Json converters from ActDim.Three into standalone assembly ActDim.Three.NewtonsoftJson and updated documentation.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Extract ActDim.Three.NewtonsoftJson

## Summary of Changes
- Created new project `ActDim.Three.NewtonsoftJson` targeting `.NET 10.0`.
- Extracted Newtonsoft converters (`CamelCaseCustomResolver`, `BufferAttributeConverter`, `SceneDocumentConverter`, `ElementConverter`) and created `ThreeNewtonsoftSerializer` wrapper in `ActDim.Three.NewtonsoftJson`.
- Decoupled `ActDim.Three` from `Newtonsoft.Json` dependency; core `ActDim.Three` relies natively on `System.Text.Json` (STJ) with typed primitive array buffers (`Float32Array`, `Uint32Array`, etc.).
- Updated `ActDim.Three.sln` and test references in `ActDim.Three.Tests`.
- Added comprehensive `README.md` for `ActDim.Three.NewtonsoftJson` and updated `ActDim.Three/README.md` and root `README.md`.

## Files Touched
- `ActDim.Three.NewtonsoftJson/ActDim.Three.NewtonsoftJson.csproj`
- `ActDim.Three.NewtonsoftJson/CamelCaseCustomResolver.cs`
- `ActDim.Three.NewtonsoftJson/BufferAttributeConverter.cs`
- `ActDim.Three.NewtonsoftJson/ElementConverter.cs`
- `ActDim.Three.NewtonsoftJson/SceneDocumentConverter.cs`
- `ActDim.Three.NewtonsoftJson/TypedArrayNewtonsoftWriteExtensions.cs`
- `ActDim.Three.NewtonsoftJson/ThreeNewtonsoftSerializer.cs`
- `ActDim.Three.NewtonsoftJson/README.md`
- `ActDim.Three/ActDim.Three.csproj`
- `ActDim.Three/Serialization/SceneDocument.cs`
- `ActDim.Three/Serialization/DocumentGraph.cs`
- `ActDim.Three/Core/Buffers/TypedArrays.cs`
- `ActDim.Three/Extensions/TypedArrayWriteExtensions.cs`
- `ActDim.Three/Textures/Texture.cs`
- `ActDim.Three/README.md`
- `ActDim.Three.sln`
- `Tests/Three.Tests/ActDim.Three.Tests.csproj`
- `Tests/Three.Tests/DocumentTests.cs`
- `Tests/Three.Tests/DeserializationTests.cs`
- `Tests/Three.Tests/GeometryDataTests.cs`
- `Tests/Three.Tests/ReferenceResolutionTests.cs`
- `Tests/Three.Tests/TypedArrayTests.cs`
- `README.md`

## Verification
- All 35 tests in `ActDim.Three.sln` passed (100% passing).
- All 560 tests in `ActDim.Practix.sln` passed (100% passing).
