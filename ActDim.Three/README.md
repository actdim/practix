# ActDim.Three

`ActDim.Three` is a high-performance 3D graphics math, geometry, scene graph, and serialization library for .NET inspired by standard three.js data structures and workflows.

## Features

- **Linear Algebra & 3D Math Primitives:** High-performance 3D math types (`Vector3`, `Matrix4`, `Euler`, `Quaternion`, `Color`).
- **Scene Graph Hierarchy:** `Scene`, `Element`, `BufferGeometry`, `Camera` (`PerspectiveCamera`, `OrthographicCamera`), and Lights (`AmbientLight`, `DirectionalLight`, `PointLight`, `SpotLight`, `HemisphereLight`, `RectAreaLight`).
- **Standard Materials:** `MeshBasicMaterial`, `MeshStandardMaterial`, `MeshPhongMaterial`, `MeshLambertMaterial`, `LineBasicMaterial`, `PointsMaterial`.
- **High-Performance Typed Arrays:** Direct primitive buffer abstractions (`Float32Array`, `Float64Array`, `Int32Array`, `Uint8Array`, `Uint8ClampedArray`).
- **Native System.Text.Json Serialization:** Native high-speed `ThreeSerializer` utilizing `System.Text.Json` (STJ) and zero-allocation UTF-8 byte stream / string operations with typed primitive arrays for maximum throughput.
- **Decoupled Architecture:** Core library has zero external dependencies on legacy serialization engines.

## Feature Support & Roadmap Status

The table below outlines current component support in `ActDim.Three` and planned features on our roadmap for Three.js JSON Object/Scene Format v4 compatibility:

| Category | Component / Feature | Current Status | Roadmap Notes |
| :--- | :--- | :---: | :--- |
| **Math Primitives** | `Vector3`, `Vector2`, `Vector4`, `Matrix4`, `Euler`, `Quaternion`, `Color` | ✅ Supported | Core 3D math & matrix transformations |
| **Scene Graph Nodes** | `Scene`, `Group`, `Mesh`, `Points`, `Line`, `LineSegments` | ✅ Supported | Core scene graph node hierarchy |
| | `InstancedMesh`, `Sprite`, `LOD`, `LineLoop` | ✅ Supported | Node extensions for instancing, sprites, and LOD |
| | `SkinnedMesh`, `Bone`, `Skeleton` | ✅ Supported | Skeletal animation node DTOs |
| | `Layers` (32-bit visibility mask) | ✅ Supported | Bitmask filtering property on `Object3D` |
| **Cameras** | `PerspectiveCamera`, `OrthographicCamera` | ✅ Supported | Camera projection matrices |
| **Lights & Shadows** | `AmbientLight`, `DirectionalLight`, `PointLight`, `SpotLight`, `HemisphereLight`, `RectAreaLight` | ✅ Supported | Complete Three.js light set |
| | `LightShadow`, `DirectionalLightShadow`, `SpotLightShadow` | ✅ Supported | Shadow map parameters |
| **Materials** | `MeshStandardMaterial`, `MeshBasicMaterial`, `MeshPhongMaterial`, `MeshLambertMaterial`, `LineBasicMaterial`, `PointsMaterial` | ✅ Supported | Standard materials |
| | `MeshPhysicalMaterial`, `LineDashedMaterial`, `MeshToonMaterial`, `ShadowMaterial`, `SpriteMaterial` | ✅ Supported | Advanced PBR, dashed line & sprite materials |
| | `ShaderMaterial`, `RawShaderMaterial`, `MeshDepthMaterial`, `MeshNormalMaterial` | ✅ Supported | GLSL shader code & depth/normal materials |
| **Buffers & Geometry** | `BufferGeometry`, `BufferAttribute`, `TypedArray` (`Float32Array`, `Uint32Array`, etc.) | ✅ Supported | Zero-allocation buffer attributes |
| | `InstancedBufferAttribute`, `InstancedBufferGeometry` | ✅ Supported | Instanced rendering attributes & geometry |
| | `InterleavedBuffer`, `InterleavedBufferAttribute`, `InstancedInterleavedBuffer` | ✅ Supported | Interleaved memory buffers |
| | Parametric Shape DTOs (`BoxGeometry`, `SphereGeometry`, `CylinderGeometry`, `PlaneGeometry`) | ✅ Supported | Parametric geometry descriptors |
| **Textures & Media** | `Texture`, `Image` | ✅ Supported | Standard 2D texture maps & images |
| | `DataTexture`, `CompressedTexture` | ✅ Supported | Raw pixel buffers & compressed textures |
| **Serialization** | `System.Text.Json` (Native Engine) | ✅ Supported | High-speed zero-allocation STJ engine |
| | `Newtonsoft.Json` (Adapter) | ✅ Supported | Via [`ActDim.Three.NewtonsoftJson`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Three.NewtonsoftJson/README.md) |

## Performance & System.Text.Json

`ActDim.Three` uses **`System.Text.Json`** natively for scene and geometry serialization.
- **Typed Array Optimization:** Numeric vertex, normal, and UV buffer data (`Float32Array`, `Uint32Array`, etc.) are written directly to/from UTF-8 byte streams without per-element object boxing (`object[]`) or intermediate string conversions.
- **High Throughput:** `System.Text.Json` provides significantly higher serialization and deserialization speeds compared to traditional reflections-heavy wrappers.

> [!NOTE]
> If your application requires **Newtonsoft.Json** compatibility, use the dedicated extension package [`ActDim.Three.NewtonsoftJson`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Three.NewtonsoftJson/README.md).

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Three
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Three
```

## Quick Start Examples

### 1. Scene & Camera Setup

```csharp
using ActDim.Three.Cameras;
using ActDim.Three.Core;
using ActDim.Three.Geometries;
using ActDim.Three.Lights;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.Scenes;

// Create 3D scene
var scene = new Scene();

// Add ambient light and directional light
var ambientLight = new AmbientLight(new Color(0x404040));
scene.Add(ambientLight);

var dirLight = new DirectionalLight(new Color(0xffffff), 1.0f);
dirLight.Position.Set(5, 10, 7.5f);
scene.Add(dirLight);

// Create perspective camera
var camera = new PerspectiveCamera(75.0f, 16.0f / 9.0f, 0.1f, 1000.0f);
camera.Position.Set(0, 0, 5);
```

### 2. Scene Graph Serialization (System.Text.Json)

```csharp
using ActDim.Three;
using ActDim.Three.Scenes;

var scene = new Scene();
// ... configure scene geometry & materials ...

// Serialize 3D scene document to JSON string using native STJ engine
string jsonString = ThreeSerializer.ToJson(scene.ToSceneDocument());

// Deserialize 3D scene document from JSON string
SceneDocument doc = ThreeSerializer.FromJson<SceneDocument>(jsonString);
```

## Testing & Quality

- **Test Suite:** `ActDim.Three.Tests`
- **Total Tests:** 39 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Three.Tests/ActDim.Three.Tests.csproj
```

## License

This project is licensed under the [MIT License](LICENSE).
