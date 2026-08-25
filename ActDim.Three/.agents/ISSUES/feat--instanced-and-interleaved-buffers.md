---
slug: instanced-and-interleaved-buffers
type: feat
status: open
priority: medium
created: 2026-08-25
updated: 2026-08-25
---

# Feature: Instanced & Interleaved Buffer Support in ActDim.Three

## Goal
Implement full support for Three.js instancing and interleaved buffer data structures in `ActDim.Three` and serialization adapters (`ActDim.Three.NewtonsoftJson`).

## Types to Implement
1. **`InstancedBufferAttribute`**: Subclass of `BufferAttribute` with `MeshPerAttribute` (`int`, default 1) for instanced rendering data.
2. **`InstancedBufferGeometry`**: Subclass of `BufferGeometry` with `InstanceCount` (`int?`) for defining instanced 3D geometries.
3. **`InterleavedBuffer`**: Shared primitive buffer (`ITypedArray`) storing interleaved vertex attributes with `Stride` (`int`), `Usage` (`int`), and identity UUID.
4. **`InterleavedBufferAttribute`**: Attribute pointing to an `InterleavedBuffer` by UUID/data reference with `Offset` (`int`), `ItemSize` (`int`), and `Normalized` (`bool`).
5. **`InstancedInterleavedBuffer`**: Subclass of `InterleavedBuffer` with `MeshPerAttribute`.

## Key Technical Steps
- **Domain Models**: Create classes under `ActDim.Three.Core.Buffers` and `ActDim.Three.Geometries`.
- **Type Registration**: Register new types in `DocumentGraph` (`NodeType` and `ElementType` maps).
- **Serialization**:
  - Add System.Text.Json converters in `ActDim.Three/Serialization/`.
  - Add Newtonsoft.Json converters in `ActDim.Three.NewtonsoftJson/`.
- **Testing**: Add unit tests in `Tests/Three.Tests` verifying creation, graph flattening, and STJ/Newtonsoft round-trip serialization.

## Acceptance Criteria
- All 5 types pass STJ and Newtonsoft round-trip serialization tests.
- Byte-level stability for Three.js JSON Object/Scene format 4 schemas.
