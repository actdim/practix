---
slug: scene-graph-data-extensions
type: feat
status: open
priority: medium
created: 2026-08-25
updated: 2026-08-25
---

# Feature: Scene Graph Data Extensions in ActDim.Three

## Goal
Implement data-facing Three.js scene graph node types (`Object3D` derivatives and textures) for C# backend data management and JSON serialization to JS clients.

## Target Node & Resource Types
1. **`InstancedMesh`**: Node for instanced mesh rendering (`InstanceMatrix` buffer, `InstanceColor` buffer, `Count`).
2. **`Sprite`**: 2D billboard node in 3D scene graph carrying `SpriteMaterial`.
3. **`LOD` (Level of Detail)**: Node managing discrete levels of detail (`Levels: [{ object, distance, hysteresis }]`).
4. **`LineLoop`**: Closed loop line node extending `Line`.
5. **`SkinnedMesh` & `Bone` / `Skeleton`**: Skeletal animation node carrying bones and bind matrices (`BindMatrix`, `BindMode`).
6. **`DataTexture` & `CompressedTexture`**: Raw pixel / compressed texture resources (`Data`, `Width`, `Height`, `Format`, `Type`).
7. **`Layers`**: Bitmask property (`mask: uint`, channels 0..31) on `Object3D` for selective camera/light visibility filtering in Three.js.

## Technical Approach
- Define attribute-free DTO data classes under `ActDim.Three.Objects` and `ActDim.Three.Textures`.
- Register type discriminators in `DocumentGraph.cs`.
- Ensure JSON Object format 4 serialization / deserialization in `System.Text.Json` and `Newtonsoft.Json`.
- Add unit tests in `Tests/Three.Tests`.

## Acceptance Criteria
- All target nodes round-trip serialize to/from JSON.
- Fully compatible with `THREE.ObjectLoader` on JS clients.
