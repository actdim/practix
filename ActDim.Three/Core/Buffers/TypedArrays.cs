using System;
using System.Collections.Generic;
using System.Globalization;

namespace ActDim.Three.Core.Buffers
{
    /// <summary>
    /// Registry mapping the three.js TypedArray discriminator string to a concrete <see cref="ITypedArray"/>.
    /// Values are materialized straight into a backing array of the target type — no intermediate buffer.
    /// The JSON path (<see cref="FromDoubles"/>) narrows each already-<c>double</c> value with a plain cast;
    /// the arbitrary-<see cref="Array"/> path (<see cref="FromArray"/>) converts each boxed element straight
    /// to the target type via <see cref="Convert"/>, with a zero-copy fast path when the source already is
    /// that primitive array.
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

        /// <summary>Custom (non-three.js) discriminator for a <c>string[]</c> buffer — see <c>StringArray</c>.</summary>
        public const string StringArray = "StringArray";

        private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
        {
            Int8Array, Uint8Array, Uint8ClampedArray, Int16Array, Uint16Array,
            Int32Array, Uint32Array, Float16Array, Float32Array, Float64Array, StringArray,
        };

        public static bool IsKnown(string type) => type != null && Known.Contains(type);

        /// <summary>Materializes a <see cref="Buffers.StringArray"/> from a sequence of strings (custom buffer).</summary>
        public static ITypedArray FromStrings(IReadOnlyList<string> values)
        {
            var count = values?.Count ?? 0;
            var data = new string[count];
            for (var i = 0; i < count; i++)
            {
                data[i] = values[i];
            }
            return new StringArray { Data = data };
        }

        /// <summary>
        /// Materializes a typed buffer from a flat number sequence: each value is already a <c>double</c>, so
        /// it is narrowed to the target type with a plain cast (three.js/JS truncation-on-overflow semantics).
        /// Throws on an unknown type.
        /// </summary>
        public static ITypedArray FromDoubles(string type, IReadOnlyList<double> values)
        {
            values ??= [];

            ITypedArray Fill<T>(Func<double, T> cast, Func<T[], ITypedArray> wrap)
            {
                var data = new T[values.Count];
                for (var i = 0; i < values.Count; i++)
                {
                    data[i] = cast(values[i]);
                }
                return wrap(data);
            }

            return type switch
            {
                Int8Array => Fill(static d => (sbyte)d, static a => new Int8Array { Data = a }),
                Uint8Array => Fill(static d => (byte)d, static a => new Uint8Array { Data = a }),
                Uint8ClampedArray => Fill(static d => (byte)d, static a => new Uint8ClampedArray { Data = a }),
                Int16Array => Fill(static d => (short)d, static a => new Int16Array { Data = a }),
                Uint16Array => Fill(static d => (ushort)d, static a => new Uint16Array { Data = a }),
                Int32Array => Fill(static d => (int)d, static a => new Int32Array { Data = a }),
                Uint32Array => Fill(static d => (uint)d, static a => new Uint32Array { Data = a }),
                Float16Array => Fill(static d => (Half)(float)d, static a => new Float16Array { Data = a }),
                Float32Array => Fill(static d => (float)d, static a => new Float32Array { Data = a }),
                Float64Array => Fill(static d => d, static a => new Float64Array { Data = a }),
                _ => throw new InvalidOperationException($"Unknown buffer attribute type '{type}'."),
            };
        }

        /// <summary>
        /// Builds a typed buffer from an arbitrary <see cref="Array"/>. Each boxed element is converted
        /// straight to the target type via <see cref="Convert"/> (no intermediate <c>double</c>). If the
        /// source already is that exact primitive array (e.g. a <c>float[]</c> for <see cref="Float32Array"/>),
        /// it is adopted as-is with no conversion and no copy. Out-of-range values throw
        /// <see cref="OverflowException"/> (unlike the truncating JSON path). Throws on an unknown type.
        /// </summary>
        public static ITypedArray FromArray(string type, Array source)
        {
            var count = source?.Length ?? 0;

            ITypedArray Fill<T>(Func<object, T> convert, Func<T[], ITypedArray> wrap)
            {
                if (source is T[] owned)
                {
                    return wrap(owned);
                }
                var data = new T[count];
                for (var i = 0; i < count; i++)
                {
                    data[i] = convert(source.GetValue(i));
                }
                return wrap(data);
            }

            var ci = CultureInfo.InvariantCulture;
            return type switch
            {
                Int8Array => Fill(o => Convert.ToSByte(o, ci), static a => new Int8Array { Data = a }),
                Uint8Array => Fill(o => Convert.ToByte(o, ci), static a => new Uint8Array { Data = a }),
                Uint8ClampedArray => Fill(o => Convert.ToByte(o, ci), static a => new Uint8ClampedArray { Data = a }),
                Int16Array => Fill(o => Convert.ToInt16(o, ci), static a => new Int16Array { Data = a }),
                Uint16Array => Fill(o => Convert.ToUInt16(o, ci), static a => new Uint16Array { Data = a }),
                Int32Array => Fill(o => Convert.ToInt32(o, ci), static a => new Int32Array { Data = a }),
                Uint32Array => Fill(o => Convert.ToUInt32(o, ci), static a => new Uint32Array { Data = a }),
                Float16Array => Fill(o => (Half)Convert.ToSingle(o, ci), static a => new Float16Array { Data = a }),
                Float32Array => Fill(o => Convert.ToSingle(o, ci), static a => new Float32Array { Data = a }),
                Float64Array => Fill(o => Convert.ToDouble(o, ci), static a => new Float64Array { Data = a }),
                StringArray => Fill(o => Convert.ToString(o, ci), static a => new StringArray { Data = a }),
                _ => throw new InvalidOperationException($"Unknown buffer attribute type '{type}'."),
            };
        }
    }
}
