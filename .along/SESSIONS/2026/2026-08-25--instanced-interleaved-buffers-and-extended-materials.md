---
protocol: along
date: 2026-08-25
slug: instanced-interleaved-buffers-and-extended-materials
agent: gemini-3.6-flash
branch: main
commit: HEAD
summary: Implemented Layers bitmask, Instanced & Interleaved buffers, Extended Materials, Scene Graph Data Nodes, and bumped solution version to 1.0.9.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Implement Extended Three.js Types & Bump Version to 1.0.9

## Summary of Changes
- **Version Bump**: Updated central `Directory.Build.props` `<Version>` to `1.0.9`.
- **Layers Bitmask**: Added `Layers` (`uint`, default 1) property and `EnableLayer`, `DisableLayer`, `ToggleLayer`, `IsOnLayer` helpers to `Object3D`.
- **Instanced & Interleaved Buffers**:
  - Implemented `InstancedBufferAttribute` and `InstancedBufferGeometry`.
  - Implemented `InterleavedBuffer`, `InterleavedBufferAttribute`, and `InstancedInterleavedBuffer`.
- **Extended Materials**:
  - Implemented `MeshPhysicalMaterial` (advanced PBR), `LineDashedMaterial`, `MeshToonMaterial`, `ShadowMaterial`, `SpriteMaterial`, `MeshDepthMaterial`, `MeshNormalMaterial`, `ShaderMaterial`, and `RawShaderMaterial`.
- **Scene Graph Nodes & Resources**:
  - Implemented `InstancedMesh`, `Sprite`, `LOD`, `LineLoop`, `SkinnedMesh`, `Bone`, `Skeleton`, `DataTexture`, `CompressedTexture`, and parametric DTO geometries (`BoxGeometry`, `SphereGeometry`, `CylinderGeometry`, `PlaneGeometry`).
- **Serialization & Support Table**:
  - Added full support in STJ engine and Newtonsoft.Json adapter.
  - Updated `ActDim.Three/README.md` status table marking all features as Supported.
  - Added unit tests in `ExtendedTypesTests.cs`.

## Files Touched
- `Directory.Build.props`
- `ActDim.Three/Core/Object3D.cs`
- `ActDim.Three/Core/InstancedBufferAttribute.cs`
- `ActDim.Three/Core/InterleavedBuffer.cs`
- `ActDim.Three/Core/InterleavedBufferAttribute.cs`
- `ActDim.Three/Core/InstancedInterleavedBuffer.cs`
- `ActDim.Three/Geometries/InstancedBufferGeometry.cs`
- `ActDim.Three/Geometries/BoxGeometry.cs`
- `ActDim.Three/Geometries/SphereGeometry.cs`
- `ActDim.Three/Geometries/CylinderGeometry.cs`
- `ActDim.Three/Geometries/PlaneGeometry.cs`
- `ActDim.Three/Materials/MeshPhysicalMaterial.cs`
- `ActDim.Three/Materials/LineDashedMaterial.cs`
- `ActDim.Three/Materials/MeshToonMaterial.cs`
- `ActDim.Three/Materials/MeshDepthMaterial.cs`
- `ActDim.Three/Materials/MeshNormalMaterial.cs`
- `ActDim.Three/Materials/ShadowMaterial.cs`
- `ActDim.Three/Materials/SpriteMaterial.cs`
- `ActDim.Three/Materials/ShaderMaterial.cs`
- `ActDim.Three/Materials/RawShaderMaterial.cs`
- `ActDim.Three/Objects/InstancedMesh.cs`
- `ActDim.Three/Objects/Sprite.cs`
- `ActDim.Three/Objects/LOD.cs`
- `ActDim.Three/Objects/LineLoop.cs`
- `ActDim.Three/Objects/SkinnedMesh.cs`
- `ActDim.Three/Objects/Bone.cs`
- `ActDim.Three/Objects/Skeleton.cs`
- `ActDim.Three/Textures/DataTexture.cs`
- `ActDim.Three/Textures/CompressedTexture.cs`
- `ActDim.Three/Serialization/DocumentGraph.cs`
- `ActDim.Three/README.md`
- `ActDim.Three/.agents/ISSUES.md`
- `Tests/Three.Tests/ExtendedTypesTests.cs`
- `Tests/Three.Tests/CommonTests.cs`

## Verification
- `ActDim.Three.sln`: 39 / 39 tests passing (100% success).
- `ActDim.Practix.sln`: 560 / 560 tests passing (100% success).
- Total Solution Tests: 599 / 599 passing across all target frameworks.
