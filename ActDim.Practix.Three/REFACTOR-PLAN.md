# Refactor plan — serialization & BufferAttribute

> Living design doc. Consolidates the decisions made while discussing how the three.js format is
> (de)serialized. Delete once the work lands.

## Progress

- ✅ **Milestone 1 — typed buffers + no-boxing BufferAttribute + deserialization** (§1, §1b, §2, §8-core):
  `Core/Buffers/TypedArrays.cs` (`ITypedArray`/`TypedArray<T>` + 10 concrete types + registry);
  `BufferAttribute` reworked to hold `ITypedArray Values` (no `System.Array`, no `object[]`), with the
  raw-`Array` copy ctor + typed factories; `BufferAttributeConverter` (typed read via `ReadAsDouble`,
  typed write); `ElementConverter` (`type`-discriminator for `IElement` pools). Both converters wired
  into `Utilities` **temporarily**. `CanDeserializeObject3D` now **passes** (+ boxing-regression guard);
  `CanSerializeComplexScene` still green. Deferred within this milestone: exact-presize streaming read
  (TODO in converter), and the `ElementConverter` still buffers via `JObject` (boxes transiently on read
  — real fix in §8/§12 streaming converter).
- ✅ **Milestone 2 (slice A) — `SceneDocument` + document converter + §11** (§3, §11, §9-UserData, part of §4/§8):
  `Serialization/SceneDocument.cs` (`SceneDocument` format type + `ToSceneDocument()` extension +
  `ThreeJson` serializer entry) and `SceneDocumentConverter` (hybrid: hand-writes structure — metadata,
  pools, uuid refs, dedup by identity, uuid assignment — and delegates resource/node bodies to the
  serializer + camelCase resolver). Adapters (`SerializationAdapter`, `Object3D/SceneSerializationAdapter`)
  and `Object3D.ProcessChildren` + all `ToJSON` overrides **deleted**. `Guid.NewGuid()` removed from
  `Element`/`BufferAttribute` ctors (§11) — the converter assigns uuids during свёртка. `Object3D.UserData`
  → `Dictionary<string, object>` (§9). Tests migrated to `ThreeJson`/`ToSceneDocument`; added
  `Document.ToSceneDocument_FlattensGraphIntoPools_AndAssignsUuids`. 3 tests green.
- ✅ **Milestone 2 (slice B) — §8 full read + byte round-trip**: `SceneDocumentConverter` read now
  rebuilds **concrete** node types (`Scene`/`Group`/`Mesh`/… via a reflected `type→Type` map),
  resolves `geometry`/`material` uuid references back to the pooled instances (`Assert.Same`), reads the
  `matrix`, and wires `Parent`. Type-specific scalars via `Populate`; structure by hand. Unresolvable
  references throw (§12); unmodeled node types fall back to base `Object3D` (subset policy). New tests:
  concrete-type + reference-resolution asserts, and `Document_IsByteStable_AcrossRoundTrip`
  (`json → doc → json` identical). 4 tests green.
- ⏳ Remaining:
  - **§4** — delete `Utilities` fully (still used by `Font.Equals`/`Matrix4`/legacy `Geometry`; tied to §5).
  - **§5** — equality/dedup + drop `CombineHashCodes`.
  - **§3a explicit names** — ✅ core types annotated (`Element`, `Object3D`, `Scene`, `Metadata`,
    `Light` base, `BufferGeometry`/`Data`/`BoundingSphere`; `Mesh`/`Line`/`Points`/`LineSegments` already
    had `geometry`/`material`). ⏳ Remaining mechanical pass: material subclasses, concrete lights,
    cameras, textures, `Font`, legacy `Geometry` — still resolver-driven (correct camelCase) until
    annotated; then drop `CamelCaseCustomResolver`.
  - **§10** — `index`/`groups`/`morphAttributes`/`drawRange`; **§12** — streaming (avoid `JObject` buffering on read).

## Goals

1. Kill per-element **boxing** in buffer attributes (huge memory / GC blowup with Newtonsoft).
2. Make (de)serialization work with **standard Newtonsoft** calls — no `Utilities` wrappers, no
   `*SerializationAdapter` hierarchy. Types describe themselves via attributes/converters.
3. Enable round-trip **deserialization** of the three.js "Object" format (currently `CanDeserializeObject3D` fails).

---

## 1. Typed buffers instead of `System.Array` (anti-boxing)

Root cause: `BufferAttribute.Array` is `System.Array`; Newtonsoft materializes `object[]` with every
number boxed (~32 B/elem vs 4 B for `float[]`), plus millions of tiny objects → GC pressure. The C#
side also pre-boxes today (`Cast<object>().ToArray()`, `OptimizeFloats` → `IEnumerable<object>`).

- Introduce a **typed representation** (chosen: separate typed types, not raw `System.Array`):
  ```
  THREE.Core.Buffers
    interface ITypedArray            // Type (three.js string), Length, ElementType, read/write
    abstract TypedArray<T> : ITypedArray   // T[] Data
      Float32Array : TypedArray<float>     Float64Array : TypedArray<double>
      Float16Array : TypedArray<System.Half>
      Int8Array : TypedArray<sbyte>        Uint8Array : TypedArray<byte>
      Uint8ClampedArray : TypedArray<byte> (see §1a)
      Int16Array/Uint16Array/Int32Array/Uint32Array
  ```
- `BufferAttribute.Array` (`System.Array`) → `ITypedArray Values` (JSON name stays `array`).
- Do **not** support `BigInt64Array`/`BigUint64Array` (three.js loader won't build them).

### 1a. Storage (DECIDED): exact-sized `T[]`, `Span<T>` out, no `List`/`object[]`
- Final storage of attribute values = a primitive `T[]` — densest practical form (contiguous, 4 B/float,
  zero per-element boxing, GPU/SIMD-friendly). This is the optimum; do **not** default to `byte[]`+reinterpret
  or native memory (no density win, more complexity) — reserve those for a real interop need.
- Optimality is in **how it's filled**: **pre-size exactly `count * itemSize`** and stream numbers
  straight into `T[]`. Never grow a `List<T>` and never go through `object[]`/`JArray`.
- Expose values outward as `Span<T>` / `ReadOnlySpan<T>` (or `Memory<T>`) for zero-copy read/write; keep
  `T[]` as the backing field.
- LOH note: a `float[]` beyond ~21k elements lands on the Large Object Heap (inherent to any big
  contiguous buffer). If mass-parse churn shows up, use `ArrayPool<T>` for **transient** parse buffers
  only; the final array is allocated at exact size (`count` is known), so no pool for storage.

### 1b. Constructor also accepts a raw `Array` — copy as efficiently as possible
Keep an ergonomic ctor/overloads that accept an incoming `System.Array` (or `ReadOnlySpan<T>`) plus the
target `type`, and copy into the typed `T[]` backing via the fastest applicable path:
1. incoming is already the exact `T[]` → `Array.Copy` (or an explicit *take-ownership* factory to skip the copy when the caller yields the array);
2. incoming is a blittable primitive array of the same element width → `Buffer.BlockCopy` (memcpy-speed);
3. incoming is a convertible primitive array (e.g. `double[]` → `float[]`) → typed element loop, **no boxing**;
4. incoming is `object[]` (already boxed) → element-wise unbox loop (fallback; caller-induced cost, but we never re-box).

Also provide typed factories (`BufferAttribute.Float32(float[], itemSize)`, `.Uint32(uint[], …)`, …) so
normal callers never touch `System.Array`/`object[]` at all.

### 1c. Uint8 vs Uint8Clamped — DO NOT collapse
`byte[]` backs both `Uint8Array` and `Uint8ClampedArray`, and `sbyte[]` backs `Int8Array` — so
`T → type` is **not 1:1**. Therefore:

- The three.js `type` is an **explicit discriminator** (field/enum or distinct subclass overriding
  `Type`), **never inferred from the CLR element type**.
- Keep both byte-backed variants distinct so the emitted `type` string is lossless round-trip.
  Reason: `Uint8ClampedArray` has real JS write-clamp semantics; for reading/GPU it's identical, but a
  serializer must not silently rewrite the payload's `type`.

## 2. `BufferAttribute` JsonConverter (the actual anti-boxing mechanism)

- **Read**: read `type` first → instantiate the right `TypedArray<T>` → pre-size `T[]` from
  `count * itemSize` → stream numbers straight into the primitive array (`ReadAsDouble/ReadAsInt32` →
  cast to `T`). Never build `object[]` / `JArray`.
- **Write**: dispatch on the concrete `TypedArray<T>`, write with typed `writer.WriteValue(T)` in a loop.
- **Single-pass requirement**: `type` must precede `array` in the output (enforce via property order).
  Otherwise the reader must buffer (`JObject.Load`) → transient boxing on large arrays. Stream by
  default; keep a buffered fallback for foreign inputs where `array` precedes `type`.
- Optional: `ArrayPool<T>` for temp buffers when `count` is unknown.

## 3. Two layers: plain domain classes + a separate `SceneData` document (DECIDED)

Drop the abstract `SerializationAdapter` / `ObjectSerializationAdapter` /
`Object3DSerializationAdapter` / `SceneSerializationAdapter` chain. Replace with a clean split:

### 3a. Explicit `[DataMember(Name=...)]` names on core types (REVISED — supersedes the earlier "attribute-free" idea)
The three.js field names ARE a fixed contract with the client lib, so they are declared **explicitly on
the types** via `[DataMember(Name="...")]` rather than derived by a resolver. Rationale: explicit,
self-describing, and portable (Newtonsoft honors `[DataMember]` natively).

- Serialized members: `[DataMember(Name="threejsName")]` with the exact three.js name.
- Excluded members (`Parent`, `Matrix`, helper `Position`/`Rotation`/`Quaternion`/`Scale`): keep
  `[IgnoreDataMember]` **for readability** (explicit "this is not serialized"), even though opt-in
  `[DataContract]` types would exclude them anyway.
- The `SceneDocument` converter still owns document STRUCTURE (pools, uuid refs, dedup — §3b); member
  bodies serialize by reflection honoring these `[DataMember]` names.
- **STJ caveat**: System.Text.Json does **not** honor `[DataMember]`/`[DataContract]` natively. If STJ
  support is wanted later, add a small DataContract-aware `IJsonTypeInfoResolver` (one-time); do NOT
  expect STJ to read these attributes out of the box.
- The `CamelCaseCustomResolver` stays for now (explicit `[DataMember(Name)]` overrides it per-property;
  it still yields correct names for the not-yet-annotated tail). Drop it once every serialized type has
  explicit names.

### 3b. `SceneDocument` — separate type for the three.js "Object" document
`SceneDocument` (name DECIDED) is its **own case**: the flat wire document, mirroring the format 1:1.

```
SceneDocument   // three.js "Object" document (a Scene OR any Object3D root)
  Metadata metadata
  List<...> geometries / materials / textures / images / [fonts]   // flat pools, by uuid
  object    // nested node tree; children reference resources by uuid string
```
- `SceneDocument` is a **format type**, not a core object → it MAY carry
  `[JsonConverter(typeof(SceneDocumentConverter))]`. That converter (plus the helper converters it calls
  for nodes / `BufferAttribute` / materials / geometry `data`) **owns every three.js rule**: it walks the
  attribute-free core graph and **explicitly writes** the exact field names (lowercase/camelCase), builds
  the flat pools, emits uuid references, dedups. The strict rules live here because they are dictated by
  the client-code contract.
- So `JsonConvert.SerializeObject(scene.ToSceneDocument())` "just works" — the converter drives it; the
  core objects stay clean. Deserialization is the same converter in reverse (§8).
- The `type`-discriminator + typed-`BufferAttribute` (no-boxing) converters are part of this converter
  set — **provided as reusable converters**, not as attributes on the core types. A standalone consumer
  can opt them into their own `JsonSerializerSettings` if they want three.js-style behavior.
- The old adapter class hierarchy and `Utilities` are deleted.

### 3c. Public API (DECIDED)
- Serialize a scene: **extension method `scene.ToSceneDocument()`** → returns a `SceneDocument`; then
  standard `JsonConvert.SerializeObject(doc)`.
- Also provide a way to build a document from a **bare set of objects**: `ToSceneDocument(this
  IEnumerable<Object3D>)` / `SceneDocument.From(params Object3D[])` — pools + `object` tree assembled
  from an `Object3D[]` even without a `Scene` root.
- `Element.ToJSON()` / `Scene.ToJSON()` wrappers and `Utilities` are removed; entry points are the
  extension methods above + plain `JsonConvert`.

### 3d. Fidelity policy (DECIDED): strict subset, NO lossless round-trip
We do **not** preserve unknown/unmodeled JSON fields (no `[JsonExtensionData]`). The C# model emits its
own subset of the three.js format; extra fields (`layers`, `renderOrder`, the long material tail,
`boundingBox`, `boundingSphere`, …) are **client-owned** and the client's responsibility, not ours.
Round-trip is lossless only for what the model covers — by design. (The modeled geometry `data` fields
are enumerated in §10.)

## 4. Delete `Utilities`

- `Serialize`/`Deserialize` wrappers → gone. Format rules do **NOT** move onto the types as attributes
  (see §3a) — they move into the `SceneDocument` converter set (§3b). Field names, pooling, ignore
  behavior are all written explicitly by the converters. `CamelCaseCustomResolver` is not needed for the
  document path (names are emitted by hand); a standalone consumer picks their own resolver/settings.
- `Flatten` → gone; superseded by typed buffer factories (callers pass real primitive arrays).
- `OptimizeFloats` → **remove**. Lossy, breaks TypedArray homogeneity (mixes int/float), and precision
  is the client's concern, not ours.
- `CombineHashCodes` → **remove** (see §5).

## 5. Equality / hashing / dedup rework

- `CombineHashCodes` is broken and wasteful:
  - `CombineHashCodes(params int[])` calls itself for an `int[]` arg → infinite recursion / stack overflow (dead).
  - Drives `Geometry/BufferGeometry.GetHashCode` + `Equals`, which power `ElementCollection.AddIfNew`
    → content dedup does `O(N)` value-equality over megabyte vertex arrays on every add, with LINQ allocs.
- Plan:
  - Remove custom hashing; use `System.HashCode` where a hash is genuinely needed.
  - Dedup resources in the flattening converter by **identity / uuid** (cheap, correct), not deep
    array equality. If content dedup is ever required, hash a cheap signature (length + sampled values
    or a precomputed content hash), not the whole array each call.

## 6. Fallout to handle (not surprises)

- `CommonTests.cs` builds attributes via `object[]` / `Cast<object>()` / `OptimizeFloats` → rewrite to
  typed factories (`BufferAttribute.Float32(float[], itemSize)` etc.).
- `CanDeserializeObject3D` (currently failing) starts passing **unchanged** once the `type`-discriminator
  converter exists: `position` → `Float32Array`, custom `colorCompact`/`id` → `Uint32Array`.
- Out of scope: `InterleavedBufferAttribute`, base64-packed buffers.

## 7. Tests to add on implementation

- Round-trip `Float32`: after deserialize, `Values` is a `Float32Array` backed by `float[]` — assert it
  is **not** `object[]` (boxing regression guard).
- Sample payload: attribute CLR/`type` mapping + values match.
- **Document round-trip (key fidelity guarantee)**: `json → SceneDocument → json` yields an equivalent
  document within the modeled subset — including **byte-stable uuids** (deserialized uuids are preserved,
  never regenerated on write; see §11). Assert pools, references, and node `type`s survive intact.
- **Standalone object (unconstrained)**: a lone core object round-trips with **default Newtonsoft +
  arbitrary consumer settings** (e.g. a custom naming policy) — proving no three.js rule is baked into the
  type and no document machinery/pooling is forced (validates §3a).
- **uuid**: (a) deserialized `Uuid` equals the JSON value; (b) a freshly built element has `Guid.Empty`
  until the document converter assigns one (validates §11 — no ctor generation).
- (Optional) perf/memory smoke on ~1M elements: peak allocation ~`4·N`, not ~`32·N`.

## 8. Read-side reconstruction (развёртка) — as big as the write side

- **Polymorphic node tree**: `object.children` is `List<Object3D>`; without a `type` discriminator it all
  deserializes to base `Object3D`, losing `Mesh`/`Group`/`Light`/`Camera`. The same `type`-discriminator
  converter used for the pools must also resolve **node types** in the tree.
- **Reference resolution (two-phase)**: load pools into uuid→object maps → build the node tree → wire
  refs: `mesh.geometry`/`mesh.material` uuid strings → live objects; `texture→image`, `material→texture`
  chains; restore `Parent` links. This is the inverse of the flattening converter and lives in the same
  `SceneDocument` layer.

## 9. Domain-model fixes required for §3a ("normally serializable")

- **`Object3D.Position` is self-recursive** (`get { return Position; }` → stack overflow on any read,
  [Object3D.cs:65](Core/Object3D.cs#L65)). Fix: make it a **plain auto-property**.
- **Transform props are INDEPENDENT of `Matrix` (DECIDED).** `Position`/`Rotation`/`Quaternion`/`Scale`
  and `Matrix` are plain, unsynced auto-properties — do **not** derive `Position` from `Matrix` or keep
  them consistent; that is not this library's job. (So the earlier "get from Matrix" idea is wrong.)
- Composing a `Matrix` is **explicit and opt-in**, .NET-style — DONE in `Math/Matrix4.cs`:
  `Matrix4.CreateScale` / `CreateFromQuaternion` / `CreateTranslation`, `operator *`, a
  `Matrix4(System.Numerics.Matrix4x4)` ctor and `ToMatrix4x4()`. Usage:
  ```csharp
  var m = Matrix4.CreateScale(scale)
        * Matrix4.CreateFromQuaternion(rotation)
        * Matrix4.CreateTranslation(position);
  ```
  Math delegates to `System.Numerics.Matrix4x4`; conversion copies `M11..M44` into the column-major
  `Elements[0..15]`, which transposes into three.js layout implicitly (translation → `te[12..14]`).
- **`UserData` shape**: today `Dictionary<string, Dictionary<string, object>>` — wrong; three.js
  `userData` is an **arbitrary JSON object** (key → scalar/array/nested object). Change to
  `Dictionary<string, object>` (nested values round-trip as `JObject`/`JArray`; pure pass-through, we
  don't interpret it — consistent with §3d). Apply consistently to both `Object3D.UserData` and
  `Material.UserData`. (`JObject` is an even more explicit "opaque blob" alternative, but the plain
  dictionary is more idiomatic; boxing is irrelevant here — small metadata, not numeric buffers.)

## 10. `BufferGeometry.data` — exactly which fields we (de)serialize

Everything below lives under `data` (except the attribute keys which live under `data.attributes`).

**Attributes (`data.attributes`, each a typed `BufferAttribute` — §1/§2 treatment):**
- ✅ `position` · ✅ `normal` · ✅ `uv` · ✅ `color` — standard vertex attributes.

**Serialized, NOT in `attributes`:**
- ✅ **`index`** — a `BufferAttribute` directly under `data` (sibling of `attributes`), `itemSize:1`,
  `type` = `Uint16Array` (≤65535 verts) or `Uint32Array`. Triangle connectivity (every 3 indices = a
  triangle). Read/write at the `data` level, same typed-array / no-boxing treatment.
- ✅ **`groups`** — `[{ start, count, materialIndex }]`. Splits the index buffer into triangle ranges,
  each drawn with a different material → pairs with **multi-material meshes** (`Mesh.material` becomes a
  `Material[]`, `materialIndex` selects into it). Usually empty or a single group. Plain POCO list, no
  buffers. NOTE: implies the domain `Mesh.Material` must also allow an array form when groups are used.
- ✅ **`morphAttributes`** — dict `{ position: [BufferAttribute, …], normal: [BufferAttribute, …], … }`;
  morph targets / blend shapes (base + weighted deltas, e.g. face smile/sad). Each entry is a **full
  `BufferAttribute`**, so every morph target gets the same typed-buffer / no-boxing path. Only present on
  animated/imported models.
- ✅ **`drawRange`** — `{ start, count }`. Render only a slice of the index buffer (progressive load,
  build-in animation, partial display). Default is `{ start:0, count:Infinity }` → three.js omits it when
  default; we serialize only when non-default.

**NOT serialized (client-owned, see §3d):**
- ❌ **`boundingSphere`** — `{ center, radius }`. May still exist on the C# side for internal use, but is
  **not** written/read. (Reverses the earlier "make it public" note.)
- ❌ **`boundingBox`** — `{ min, max }`. Not modeled, not serialized.

**Parse sizing:** `count` may be absent from foreign producers — pre-size `T[]` from `count*itemSize`
when present, else grow a primitive buffer with a final trim (never `List<object>`).

## 11. UUID strategy — stop generating in the constructor (DECIDED)

Problem: `Element` and `BufferAttribute` ctors call `Guid.NewGuid()`. That means non-deterministic
output (new uuids every run), wasted work when the object is about to be deserialized and overwritten, a
hidden side effect in construction, and it fights stable round-trip.

uuid is a **wire-format concern** (pools reference resources by uuid), so per §3a it belongs to the
document layer, not the domain ctor.

- Core `Uuid` becomes a plain property defaulting to `Guid.Empty` — **no generation in the ctor**.
- The `SceneDocument` converter, while flattening, **assigns a fresh uuid to any element still
  `Guid.Empty`**, and dedups shared resources by **reference identity** (§5). Consumer-set uuids are
  honored, not overwritten.
- On read, uuid comes from JSON; on write, existing uuids are **preserved, never regenerated** → this is
  what makes the §7 document round-trip byte-stable.
- Optional `EnsureUuid()` helper for callers who want to pre-assign explicitly.

## 12. Read robustness & document I/O policy

- **Malformed references / unknown types**: an unresolvable uuid reference (`mesh.geometry` points at no
  pool entry) or an unknown node/resource `type` → **throw with a clear message** by default (the format
  is machine-generated; silent-skip hides producer bugs). Revisit a lenient mode only if a real need appears.
- **Streaming, not buffering**: the `SceneDocument` converter reads/writes through `JsonReader`/
  `JsonWriter`, **not** `JObject.Load`, so large scenes don't spike memory — consistent with the no-boxing
  streaming of §2. (Buffering is acceptable only for small sub-objects where field order forces it.)
- **`metadata`**: emit `version` (4.5) + a `generator` string on write; on read, validate
  `metadata.type == "Object"` (reject unrelated three.js payloads early).
