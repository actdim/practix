---
slug: extended-materials-and-lights
type: feat
status: open
priority: medium
created: 2026-08-25
updated: 2026-08-25
---

# Feature: Extended Materials & Shader Material Support in ActDim.Three

## Goal
Expand `ActDim.Three` material ecosystem to cover the remaining standard Three.js material types for seamless C# backend -> JS frontend scene graph transmission.

## Materials to Implement
1. **`MeshPhysicalMaterial`**: Extension of `MeshStandardMaterial` with PBR properties (`Clearcoat`, `ClearcoatRoughness`, `Transmission`, `Thickness`, `Ior`, `Sheen`, `Iridescence`).
2. **`LineDashedMaterial`**: Material for dashed 2D/3D lines (`DashSize`, `GapSize`, `Scale`).
3. **`MeshToonMaterial`**: Stylized cel-shading material with gradient map support (`GradientMap`).
4. **`MeshDepthMaterial` & `MeshNormalMaterial`**: Materials for depth and normal buffer visualization and post-processing passes.
5. **`ShadowMaterial`**: Shadow receiving transparent material for AR floor planes (`Opacity`).
6. **`SpriteMaterial`**: Billboard sprite material (`Map`, `Rotation`, `SizeAttenuation`).
7. **`ShaderMaterial` & `RawShaderMaterial`**: Custom GLSL shader materials with serializable `Uniforms`, `VertexShader`, and `FragmentShader`.

## Key Technical Steps
- Create domain models under `ActDim.Three.Materials`.
- Update `DocumentGraph.cs` element type discriminator maps.
- Ensure `SceneDocumentStjConverter` and `SceneDocumentConverter` (Newtonsoft) polymorphically deserialize and serialize these materials.
- Add unit tests in `Tests/Three.Tests` for each material type.

## Acceptance Criteria
- All materials deserialize correctly from standard Three.js JSON Object format 4.
- Round-trip serialization tests pass for both System.Text.Json and Newtonsoft.Json.
