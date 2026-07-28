using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace THREE.Core.Buffers
{
    /// <summary>
    /// A homogeneous numeric buffer that mirrors a JS TypedArray. The concrete implementation carries a
    /// primitive <c>T[]</c> backing (no per-element boxing) and reports the three.js <see cref="Type"/>
    /// string explicitly (the CLR element type is not enough — e.g. both <see cref="Uint8Array"/> and
    /// <see cref="Uint8ClampedArray"/> are backed by <c>byte[]</c>).
    /// </summary>
    public interface ITypedArray
    {
        /// <summary>three.js TypedArray discriminator, e.g. "Float32Array".</summary>
        string Type { get; }

        /// <summary>Number of elements in the backing array.</summary>
        int Length { get; }

        /// <summary>The primitive backing array (for inspection / equality).</summary>
        Array Data { get; }

        /// <summary>Writes the flat JSON number array using typed overloads (no boxing).</summary>
        void Write(JsonWriter writer);
    }

    /// <summary>Base class for a typed numeric buffer backed by a primitive <typeparamref name="T"/> array.</summary>
    public abstract class TypedArray<T> : ITypedArray
    {
        public T[] Data { get; set; }

        public abstract string Type { get; }

        public int Length => Data?.Length ?? 0;

        Array ITypedArray.Data => Data;

        public abstract void Write(JsonWriter writer);
    }

    public sealed class Int8Array : TypedArray<sbyte>
    {
        public override string Type => TypedArrays.Int8Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Uint8Array : TypedArray<byte>
    {
        public override string Type => TypedArrays.Uint8Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Uint8ClampedArray : TypedArray<byte>
    {
        public override string Type => TypedArrays.Uint8ClampedArray;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Int16Array : TypedArray<short>
    {
        public override string Type => TypedArrays.Int16Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Uint16Array : TypedArray<ushort>
    {
        public override string Type => TypedArrays.Uint16Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Int32Array : TypedArray<int>
    {
        public override string Type => TypedArrays.Int32Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Uint32Array : TypedArray<uint>
    {
        public override string Type => TypedArrays.Uint32Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Float16Array : TypedArray<Half>
    {
        public override string Type => TypedArrays.Float16Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue((float)v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Float32Array : TypedArray<float>
    {
        public override string Type => TypedArrays.Float32Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    public sealed class Float64Array : TypedArray<double>
    {
        public override string Type => TypedArrays.Float64Array;
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var v in Data)
            {
                writer.WriteValue(v);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Registry mapping the three.js TypedArray discriminator string to a concrete <see cref="ITypedArray"/>.
    /// Materializes a typed primitive array from a flat sequence of numbers without per-element object boxing.
    /// </summary>
    public static class TypedArrays
    {
        public const string Int8Array = "Int8Array";
        public const string Uint8Array = "Uint8Array";
        public const string Uint8ClampedArray = "Uint8ClampedArray";
        public const string Int16Array = "Int16Array";
        public const string Uint16Array = "Uint16Array";
        public const string Int32Array = "Int32Array";
        public const string Uint32Array = "Uint32Array";
        public const string Float16Array = "Float16Array";
        public const string Float32Array = "Float32Array";
        public const string Float64Array = "Float64Array";

        private static readonly Dictionary<string, Func<IReadOnlyList<double>, ITypedArray>> FromDoublesMap =
            new Dictionary<string, Func<IReadOnlyList<double>, ITypedArray>>(StringComparer.Ordinal)
        {
            { Int8Array, FromDoublesInt8 },
            { Uint8Array, FromDoublesUint8 },
            { Uint8ClampedArray, FromDoublesUint8Clamped },
            { Int16Array, FromDoublesInt16 },
            { Uint16Array, FromDoublesUint16 },
            { Int32Array, FromDoublesInt32 },
            { Uint32Array, FromDoublesUint32 },
            { Float16Array, FromDoublesFloat16 },
            { Float32Array, FromDoublesFloat32 },
            { Float64Array, FromDoublesFloat64 },
        };

        private static ITypedArray FromDoublesInt8(IReadOnlyList<double> values)
        {
            var data = new sbyte[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (sbyte)values[i];
            }
            return new Int8Array { Data = data };
        }

        private static ITypedArray FromDoublesUint8(IReadOnlyList<double> values)
        {
            var data = new byte[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)values[i];
            }
            return new Uint8Array { Data = data };
        }

        private static ITypedArray FromDoublesUint8Clamped(IReadOnlyList<double> values)
        {
            var data = new byte[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)values[i];
            }
            return new Uint8ClampedArray { Data = data };
        }

        private static ITypedArray FromDoublesInt16(IReadOnlyList<double> values)
        {
            var data = new short[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (short)values[i];
            }
            return new Int16Array { Data = data };
        }

        private static ITypedArray FromDoublesUint16(IReadOnlyList<double> values)
        {
            var data = new ushort[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (ushort)values[i];
            }
            return new Uint16Array { Data = data };
        }

        private static ITypedArray FromDoublesInt32(IReadOnlyList<double> values)
        {
            var data = new int[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (int)values[i];
            }
            return new Int32Array { Data = data };
        }

        private static ITypedArray FromDoublesUint32(IReadOnlyList<double> values)
        {
            var data = new uint[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (uint)values[i];
            }
            return new Uint32Array { Data = data };
        }

        private static ITypedArray FromDoublesFloat16(IReadOnlyList<double> values)
        {
            var data = new Half[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (Half)(float)values[i];
            }
            return new Float16Array { Data = data };
        }

        private static ITypedArray FromDoublesFloat32(IReadOnlyList<double> values)
        {
            var data = new float[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (float)values[i];
            }
            return new Float32Array { Data = data };
        }

        private static ITypedArray FromDoublesFloat64(IReadOnlyList<double> values)
        {
            var data = new double[values.Count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = values[i];
            }
            return new Float64Array { Data = data };
        }

        public static bool IsKnown(string type) => type != null && FromDoublesMap.ContainsKey(type);

        /// <summary>Materializes a typed buffer from a flat number sequence. Throws on an unknown type.</summary>
        public static ITypedArray FromDoubles(string type, IReadOnlyList<double> values)
        {
            if (type == null || !FromDoublesMap.TryGetValue(type, out var factory))
            {
                throw new JsonSerializationException($"Unknown buffer attribute type '{type}'.");
            }
            return factory(values ?? Array.Empty<double>());
        }

        /// <summary>
        /// Builds a typed buffer from an arbitrary <see cref="Array"/> (convenience path; converts through
        /// <c>double</c>). Prefer the typed factories on <c>BufferAttribute</c> for large data — they take
        /// ownership of the primitive array with no copy.
        /// TODO: add fast paths (Buffer.BlockCopy for matching element widths).
        /// </summary>
        public static ITypedArray FromArray(string type, Array source)
        {
            var values = new double[source?.Length ?? 0];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Convert.ToDouble(source.GetValue(i), CultureInfo.InvariantCulture);
            }
            return FromDoubles(type, values);
        }
    }
}
