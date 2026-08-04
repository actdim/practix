using Newtonsoft.Json;
using STJ = System.Text.Json;
using THREE.Core.Buffers;

namespace THREE.Serialization
{
    /// <summary>
    /// Writes a typed buffer as a flat JSON number array using typed overloads (no per-element boxing).
    /// The serializer-specific writing lives here so the buffer types stay serializer-agnostic; one
    /// overload per serializer. A null buffer writes an empty array.
    /// </summary>
    internal static class TypedArrayWriteExtensions
    {
        public static void WriteTo(this ITypedArray values, JsonWriter writer)
        {
            writer.WriteStartArray();

            switch (values)
            {
                case Int8Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Uint8Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Uint8ClampedArray a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Int16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Uint16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Int32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Uint32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Float16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue((float)v);
                    }
                    break;
                case Float32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case Float64Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                case StringArray a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteValue(v);
                    }
                    break;
            }

            writer.WriteEndArray();
        }

        public static void WriteTo(this ITypedArray values, STJ.Utf8JsonWriter writer)
        {
            writer.WriteStartArray();

            switch (values)
            {
                case Int8Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Uint8Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Uint8ClampedArray a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Int16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Uint16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Int32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Uint32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Float16Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue((float)v);
                    }
                    break;
                case Float32Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case Float64Array a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteNumberValue(v);
                    }
                    break;
                case StringArray a:
                    foreach (var v in a.Data)
                    {
                        writer.WriteStringValue(v);
                    }
                    break;
            }

            writer.WriteEndArray();
        }
    }
}
