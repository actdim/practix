# ActDim.Three

`ActDim.Three` is a 3D graphics math, geometry, scene graph, and serialization library for .NET inspired by standard three.js data structures and workflows.

## Features

- **Linear Algebra & 3D Math Primitives:** High-performance 3D math types (`Vector3`, `Matrix4`, `Euler`, `Quaternion`, `Color`).
- **Scene Graph Hierarchy:** `Scene`, `Element`, `BufferGeometry`, `Camera` (`PerspectiveCamera`, `OrthographicCamera`), and Lights (`AmbientLight`, `DirectionalLight`, `PointLight`, `SpotLight`).
- **Standard Materials:** `MeshBasicMaterial`, `MeshStandardMaterial`, `MeshPhongMaterial`, `MeshLambertMaterial`, `LineBasicMaterial`, `PointsMaterial`.
- **High-Performance Typed Arrays:** Efficient buffer memory abstractions (`Float32Array`, `Float64Array`, `Int32Array`, `Uint8Array`, `Uint8ClampedArray`).
- **JSON Scene Serialization:** Built-in `ThreeSerializer` for serializing and deserializing 3D scene graphs (`SceneDocument`, `DocumentGraph`) compatible with JSON interchange formats.

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

### 2. Scene Graph Serialization

```csharp
using ActDim.Three.Scenes;
using ActDim.Three.Serialization;

var scene = new Scene();
// ... configure scene geometry & materials ...

// Serialize 3D scene to JSON string
string jsonString = ThreeSerializer.Serialize(scene);

// Deserialize 3D scene from JSON
Scene restoredScene = ThreeSerializer.DeserializeScene(jsonString);
```

## License

This project is licensed under the [MIT License](LICENSE).
