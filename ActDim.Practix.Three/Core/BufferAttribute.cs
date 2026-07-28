using System;
using THREE.Core.Buffers;

namespace THREE.Core
{

    public interface IBufferAttribute : IElement
    {

    }

    /// <summary>
    /// A named vertex buffer attribute. The numeric payload is a typed <see cref="ITypedArray"/>
    /// (primitive <c>T[]</c> backing, no per-element boxing). (De)serialization is handled entirely by
    /// <c>BufferAttributeConverter</c> — this type carries no serialization attributes.
    /// </summary>
    public class BufferAttribute : IBufferAttribute
    {
        public Guid Uuid { get; set; }

        public string Name { get; set; }

        public int ItemSize { get; set; }

        /// <summary>Number of vertices = backing length / <see cref="ItemSize"/>.</summary>
        public int Count => (Values != null && ItemSize > 0) ? Values.Length / ItemSize : 0;

        /// <summary>three.js TypedArray discriminator, taken from <see cref="Values"/>.</summary>
        public string Type => Values?.Type;

        public bool Normalized { get; set; }

        public bool Dynamic { get; set; }

        /// <summary>The typed numeric payload.</summary>
        public ITypedArray Values { get; set; }

        public BufferAttribute()
        {
            // uuid defaults to Guid.Empty; the document layer assigns one if needed (plan §11).
        }

        /// <summary>
        /// Builds an attribute from an arbitrary <see cref="Array"/>, copying/converting into the typed
        /// buffer selected by <paramref name="type"/>. Convenience path — see
        /// <see cref="TypedArrays.FromArray"/>.
        /// </summary>
        public BufferAttribute(string type, Array source, int itemSize, bool normalized = false) : this()
        {
            ItemSize = itemSize;
            Normalized = normalized;
            Values = TypedArrays.FromArray(type, source);
        }

        // Typed factories — take ownership of the primitive array (no copy), zero boxing.
        public static BufferAttribute Int8(sbyte[] data, int itemSize, bool normalized = false) => Make(new Int8Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Uint8(byte[] data, int itemSize, bool normalized = false) => Make(new Uint8Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Uint8Clamped(byte[] data, int itemSize, bool normalized = false) => Make(new Uint8ClampedArray { Data = data }, itemSize, normalized);
        public static BufferAttribute Int16(short[] data, int itemSize, bool normalized = false) => Make(new Int16Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Uint16(ushort[] data, int itemSize, bool normalized = false) => Make(new Uint16Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Int32(int[] data, int itemSize, bool normalized = false) => Make(new Int32Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Uint32(uint[] data, int itemSize, bool normalized = false) => Make(new Uint32Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Float16(Half[] data, int itemSize, bool normalized = false) => Make(new Float16Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Float32(float[] data, int itemSize, bool normalized = false) => Make(new Float32Array { Data = data }, itemSize, normalized);
        public static BufferAttribute Float64(double[] data, int itemSize, bool normalized = false) => Make(new Float64Array { Data = data }, itemSize, normalized);

        private static BufferAttribute Make(ITypedArray values, int itemSize, bool normalized)
        {
            return new BufferAttribute { ItemSize = itemSize, Normalized = normalized, Values = values };
        }
    }
}
