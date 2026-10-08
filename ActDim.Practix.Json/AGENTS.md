## Project specifics

JSON serialization abstraction layer implementing `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer` via `CoreJsonSerializer`, built on `System.Text.Json` with Newtonsoft.Json-familiar defaults configured out of the box.

### Architecture

- **Abstraction contracts** live in `ActDim.Practix.Abstractions` (`IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, `IStreamSerializer`).
- **`CoreJsonSerializer`** is the single implementation providing: serialize/deserialize to string, `Stream` (sync + async), and `byte[]`; JSON document merging (`MergeAndSerialize`); deep cloning (`Clone<T>`); object population and patching (`Populate`, `Patch`, `Copy`); property name formatting via naming policies; and options cloning via compiled Expression Tree delegates.
- **DI entry point:** `services.AddCoreJsonSerializer()` registers `CoreJsonSerializer` as shared singleton for `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer`.

### Newtonsoft.Json-Familiar Defaults

`CreateDefaultOptions()` configures STJ to match common Newtonsoft.Json behaviors: case-insensitive property matching, lenient number/string coercion (`AllowReadingFromString`, `AllowNamedFloatingPointLiterals`), trailing commas, comment skipping, relaxed UTF-8 encoding (`UnsafeRelaxedJsonEscaping`), public field inclusion, and cycle-safe reference handling (`IgnoreCycles`).

### Custom Converters and Resolvers

- `ObjectJsonConverter` - deserializes `object` to CLR primitives / `ExpandoObject` / `List<object>` instead of `JsonElement`.
- `NewtonsoftCompatibleStringConverter` - coerces non-string tokens (numbers, booleans, objects) into `string`.
- `NumberEnumConverterFactory` - numeric enum serialization respecting type-level `[JsonConverter]` overrides.
- `FloatingPointConverterFactory` - preserves `.0` suffix on whole floats; handles `NaN`/`Infinity` as string literals.
- `ExceptionJsonConverter` - safe structured `Exception` serialization without circular references.
- `ImplicitOperatorConverterFactory` - automatic value object serialization via `op_Implicit`/`op_Explicit` discovery.
- `NamingPolicyResolver` - per-type naming policy via `[JsonNaming]` attribute.
- `DefaultValueAwareResolver` - `[JsonIgnoreDefault]` and `[JsonDefaultValue]` attribute processing.
- `EmptyCollectionIgnoreResolver` - `[JsonIgnoreEmpty]` attribute processing.
