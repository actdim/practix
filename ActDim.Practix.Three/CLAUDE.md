# ActDim.Practix.Three

Utility for working with the [three.js](https://threejs.org/) data format: dynamically **building**,
**serializing**, and **deserializing** scenes into three.js-compatible JSON
([JSON Object/Scene format 4](https://github.com/mrdoob/three.js/wiki/JSON-Object-Scene-format-4)).

This is a .NET port of the three.js object model. The classes mirror the hierarchy and names of the
original JS library so that an object graph assembled in C# can be handed to a three.js renderer
without manual transformation.

- Project type: library (`net10.0`, `x64`, signed assembly).
- Root namespace: **`THREE`** (not `ActDim.*`), matching the original ThreeLib.
- Only external dependency: `Newtonsoft.Json` 13.x. All JSON goes through Newtonsoft,
  **not `System.Text.Json`** — when adding code, stick to Newtonsoft attributes/resolvers.

## Layout

| Folder | Contents |
|--------|----------|
| `Core/` | Base types: `Element` (Uuid/Name/Type), `Object3D` (scene graph, `Add`/`ToJSON`), `Geometry`, `BufferGeometry`, `BufferAttribute`, `ElementCollection`, `Font`. |
| `Math/` | `Vector3`, `Euler`, `Quaternion`, `Matrix4`, `Color`. |
| `Scenes/` | `Scene` (graph root + `ToJSON`), `Metadata` (version/type/generator). |
| `Objects/` | `Mesh`, `Line`, `LineSegments`, `Points`, `Group`. |
| `Geometries/` | `SphereGeometry`/`SphereBufferGeometry`, `TextGeometry`. |
| `Materials/` | `Material` and subclasses (`MeshStandardMaterial`, `MeshBasicMaterial`, `LineBasicMaterial`, `PointsMaterial`, …), `MaterialEnums`. |
| `Lights/` | `AmbientLight`, `DirectionalLight`, `PointLight`, `SpotLight`, `HemisphereLight`, `RectAreaLight` and their `*Shadow` types. |
| `Cameras/` | `PerspectiveCamera`, `OrthographicCamera`. |
| `Textures/` | `Texture`, `Image`. |
| `Serialization/` | `CamelCaseCustomResolver` — camelCase property names, but **dictionary keys are left untouched**. |
| `Utility/` | `Utilities` (Serialize/Deserialize/Flatten/OptimizeFloats), `SerializationAdapter` (shape of the emitted JSON). |

## Data model

- `Element` — base for everything with a `Uuid` (generated in the constructor), `Name`, and `Type`
  (`Type` defaults to the class name).
- `Object3D : Element` — a graph node: `Children`, `Parent`, transform (`Matrix`/`Position`/`Rotation`/`Scale`),
  `UserData`. Build the tree via `Add(child)` / `AddRange(...)` — they set `Parent`.
- `Scene : Object3D` — the root; adds `Background`.
- Geometry: legacy `Geometry` (vertices/faces/normals — see `Geometry.ProcessVertexArray` /
  `ProcessFaceArray`) and modern `BufferGeometry` (an `Attributes` dictionary of `BufferAttribute`
  with `Array`/`ItemSize`/`Type`, e.g. `"Float32Array"`, plus `BoundingSphere`).
- `Color` holds RGB; three.js writes color as an `int` — use `new Color(r,g,b).ToInt()`.

## How serialization works (important)

`Scene`/`Object3D` are **not serialized directly**. `ToJSON()` builds an intermediate
`*SerializationAdapter` (`Utility/SerializationAdapter.cs`, `Object3DSerializationAdapter`,
`SceneSerializationAdapter`) and serializes that via `Utilities.Serialize`. The adapter flattens the
graph:

- `ProcessChildren()` walks `Children` recursively, gathering **shared resources** into flat
  top-level collections — `Geometries`, `Materials`, `Textures`, `Images`, `Fonts`.
- Deduplication goes through `ElementCollection.AddIfNew`, which relies on the element's `Equals` and
  returns the existing `Uuid`. Objects in the tree reference resources by that `Uuid`.
- `Group` is collapsed: its nodes are hoisted into the root's `object.children`.

Consequences when modifying this code:
- `AddIfNew` behavior depends on a correct `Equals`/`GetHashCode` on the resource
  (`Utilities.CombineHashCodes`). Break dedup and the JSON gets duplicate geometries/materials.
- The per-type child routing (`Mesh`/`Line`/`Points`/`Group`/other) lives entirely in
  `Object3D.ProcessChildren` — add new object types there.

## Common workflows

**Create and serialize a scene:**
```csharp
var scene = new Scene { Name = "My Scene", Background = new Color(255, 0, 255).ToInt() };
scene.Add(new Mesh { Geometry = geometry, Material = MeshStandardMaterial.Default(), Name = "Cube" });

byte[] json = scene.ToJSON();                 // UTF-8 bytes of three.js-compatible JSON
string text = Encoding.UTF8.GetString(json);
```

**Low-level (de)serialization of an arbitrary type:**
```csharp
byte[] bytes = Utilities.Serialize(obj, format: true);   // Indented
var back     = Utilities.Deserialize<MyType>(bytes);
```
`Utilities.Serialize/Deserialize` share the same settings: `DefaultValueHandling.Ignore`,
`NullValueHandling.Ignore`, `CamelCaseCustomResolver`. Keep write and read paths consistent on these
settings.

**Geometry helpers:** `Utilities.Flatten(...)` unrolls nested arrays into a flat stream;
`Utilities.OptimizeFloats(...)` collapses integer-valued floats to `int` (more compact JSON).

## Conventions

- Public properties exposed to JSON are marked `[DataMember]` / `[IgnoreDataMember]`; outbound names
  are camelCase (see the resolver). Adapter field order is set via `[DataMember(Order = N)]`.
- `ShouldSerializeXxx()` (e.g. `ShouldSerializeImages`) — Newtonsoft-style conditional serialization;
  empty collections are omitted.
- Preserve the three.js names — that is a compatibility contract, not just style.

## Tests

Project `Tests/Three.Tests` (`ActDim.Practix.Three.Tests.csproj`) uses **xUnit v3**
(`xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`). Run with `dotnet test`.
`CommonTests.cs` assembles a large graph (meshes, lines, points, buffer geometries, groups, lights)
and asserts that `Scene.ToJSON()` produces non-empty JSON — a fast smoke test of the whole
serialization pipeline.
