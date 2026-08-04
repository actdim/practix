<!-- BEGIN ACTDIM-AGENTS-PROTOCOL ref=../AGENTS.md (managed by init-agents — do not edit by hand) -->
This folder belongs to a repository that uses the ACTDIM-AGENTS structure. The full working
guidance + agent-context protocol live once in the nearest ancestor `AGENTS.md` (`../AGENTS.md`) —
read it there. This folder keeps its OWN `.agents/` state; use the nearest one.
Only this folder's specifics follow.
<!-- END ACTDIM-AGENTS-PROTOCOL -->

## Project specifics

# ActDim.Practix.Three

Utility for working with the [three.js](https://threejs.org/) data format: dynamically **building**,
**serializing**, and **deserializing** scenes into three.js-compatible JSON
([JSON Object/Scene format 4](https://github.com/mrdoob/three.js/wiki/JSON-Object-Scene-format-4)).

This is a .NET port of the three.js object model. The classes mirror the hierarchy and names of the
original JS library so that an object graph assembled in C# can be handed to a three.js renderer
without manual transformation.

- Project type: library (`net10.0`, `x64`, signed assembly).
- Root namespace: **`ActDim.Three`** (not `ActDim.*`), matching the original ActDim.Three.
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

## How the client (three.js / JS) consumes this data

The JSON we emit is loaded on the client by three.js `ObjectLoader` (which delegates geometry to
`BufferGeometryLoader`, materials to `MaterialLoader`). What matters for *us* is that the **data** we
write must map cleanly onto what the client reconstructs. Refs:
[format 4 wiki](https://github.com/mrdoob/three.js/wiki/JSON-Object-Scene-format-4),
[BufferAttribute docs](https://threejs.org/docs/#api/en/core/BufferAttribute).

**Reconstruction is by UUID.** Pools (`geometries`, `materials`, `textures`, `images`) are loaded
first into lookup maps; then `object.children` are rebuilt and their `"geometry": "<uuid>"` /
`"material": "<uuid>"` string fields are resolved against those maps. So a mesh's `geometry`/`material`
in JSON is a **uuid reference**, never an inline object — this is exactly what `ProcessChildren` +
`AddIfNew` produce on the C# side.

**Buffer attributes are the critical payload.** Each entry under `data.attributes` (and `data.index`)
becomes a `ActDim.Three.BufferAttribute` on the client, built from:

- `array` — flat list of numbers, laid out per-vertex (vertex *i* occupies `array[i*itemSize .. +itemSize]`).
  On the client it is wrapped in a **JS TypedArray whose class is chosen from the `type` string**.
- `itemSize` — values per vertex (3 = position/normal, 2 = uv, 4 = rgba, 1 = index/scalar).
- `count` — vertex count; on the client it is `array.length / itemSize` (informational in JSON).
- `normalized` — for **integer** arrays only: `true` means the client remaps values at shader time —
  unsigned → `[0,1]`, signed → `[-1,1]` (e.g. color stored as `Uint8Array` normalized). Ignored for floats.
- `name` / `uuid` — carried through; three.js keys the attribute by its dictionary name (`position`,
  `normal`, `uv`, `color`, `index`, …), so the **dictionary key is what the renderer binds**, not `name`.

**`type` MUST be a valid JS TypedArray name** — the client picks both the TypedArray and the
`*BufferAttribute` subclass from it. Emitting a wrong/empty `type` (see the `// TODO: Type = ...`
spots in `CommonTests.cs`) means the client cannot reconstruct the attribute. Mapping:

| `type` in JSON | JS TypedArray | Client `ActDim.Three.*BufferAttribute` |
|----------------|---------------|---------------------------------|
| `Int8Array`         | `Int8Array`         | `Int8BufferAttribute` |
| `Uint8Array`        | `Uint8Array`        | `Uint8BufferAttribute` |
| `Uint8ClampedArray` | `Uint8ClampedArray` | `Uint8ClampedBufferAttribute` |
| `Int16Array`        | `Int16Array`        | `Int16BufferAttribute` |
| `Uint16Array`       | `Uint16Array`       | `Uint16BufferAttribute` (typical for `index` ≤ 65535 verts) |
| `Int32Array`        | `Int32Array`        | `Int32BufferAttribute` |
| `Uint32Array`       | `Uint32Array`       | `Uint32BufferAttribute` (`index` > 65535 verts) |
| `Float16Array`      | `Float16Array`      | `Float16BufferAttribute` |
| `Float32Array`      | `Float32Array`      | `Float32BufferAttribute` (default for `position`/`normal`/`uv`) |
| `Float64Array`      | `Float64Array`      | base `BufferAttribute` (no dedicated subclass) |

Notes:
- `BigInt64Array` / `BigUint64Array` are valid JS TypedArrays but three.js has **no** matching
  `*BufferAttribute` and its loader will not build them — do not emit these as attribute `type`.
- Non-standard attribute keys are fine as long as `type` is a valid TypedArray: the sample payload in
  `DeserializationTests.cs` carries custom `colorCompact` (`Uint32Array`, itemSize 1) and `id`
  (`Uint32Array`) attributes — the client will create real `Uint32BufferAttribute`s, but only a shader
  / consumer that knows those keys will use them.
- Choose the smallest type that fits the data: `index` as `Uint16Array` unless the geometry exceeds
  65535 vertices (then `Uint32Array`); colors as normalized `Uint8Array` instead of `Float32Array` to
  cut payload size. `Utilities.OptimizeFloats` compresses integer-valued floats, but the declared
  `type` is what the client actually honors.

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
