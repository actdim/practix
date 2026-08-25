using Newtonsoft.Json;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// Extension methods for writing <see cref="ITypedArray"/> instances to Newtonsoft <see cref="JsonWriter"/>.
    /// </summary>
    public static class TypedArrayNewtonsoftWriteExtensions
    {
        /// <summary>
        /// Writes a typed buffer as a flat JSON number array to a Newtonsoft <see cref="JsonWriter"/>.
        /// </summary>
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
    }
}
