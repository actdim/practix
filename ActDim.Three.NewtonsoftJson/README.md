# ActDim.Three.NewtonsoftJson

`ActDim.Three.NewtonsoftJson` is the official `Newtonsoft.Json` compatibility adapter and converter library for [`ActDim.Three`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Three/README.md).

## Overview

This library provides full support for serializing and deserializing `ActDim.Three` 3D scene graphs (`SceneDocument`), resource pools, and typed array attributes (`BufferAttribute`, `Float32Array`, `Uint32Array`, etc.) using `Newtonsoft.Json`.

It decouples `Newtonsoft.Json` dependencies from the core engine [`ActDim.Three`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Three/README.md), allowing core applications to run with native, lightweight `System.Text.Json` while offering legacy or custom `Newtonsoft.Json` integration when needed.

## Features

- **`ThreeNewtonsoftSerializer`:** High-level convenience helper for `Newtonsoft.Json` serialization and deserialization.
- **`SceneDocumentConverter`:** Custom Newtonsoft `JsonConverter` for Three.js Object/Scene JSON format 4.
- **`BufferAttributeConverter`:** High-performance converter reading and writing typed primitive arrays (`Float32Array`, `Uint32Array`, etc.) without per-element object boxing.
- **`ElementConverter`:** Polymorphic discriminator converter for heterogeneous element pools (`geometries`, `materials`, `textures`, `images`).
- **`CamelCaseCustomResolver`:** Custom contract resolver preserving dictionary keys and camelCase property conventions.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Three.NewtonsoftJson
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Three.NewtonsoftJson
```

## Quick Start Example

```csharp
using ActDim.Three;
using ActDim.Three.NewtonsoftJson;
using ActDim.Three.Scenes;

// Create or obtain a scene document
Scene scene = new Scene();
SceneDocument doc = scene.ToSceneDocument();

// 1. Serialize using ThreeNewtonsoftSerializer
string json = ThreeNewtonsoftSerializer.ToJson(doc, indented: true);

// 2. Deserialize using ThreeNewtonsoftSerializer
SceneDocument restoredDoc = ThreeNewtonsoftSerializer.FromJson<SceneDocument>(json);
```

## Testing & Quality

- **Test Suite:** `ActDim.Three.Tests`
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Three.Tests/ActDim.Three.Tests.csproj
```

## License

This project is licensed under the [MIT License](LICENSE).
