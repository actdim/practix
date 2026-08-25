using System;

namespace ActDim.Three.Core
{
    /// <summary>
    /// An attribute referencing a view into an <see cref="InterleavedBuffer"/>.
    /// Analogous to https://threejs.org/docs/#api/en/core/InterleavedBufferAttribute
    /// </summary>
    public class InterleavedBufferAttribute
    {
        public Guid Uuid { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Reference to the underlying <see cref="InterleavedBuffer"/> by UUID.
        /// </summary>
        public Guid DataUuid { get; set; }

        /// <summary>
        /// Direct object reference to the underlying <see cref="InterleavedBuffer"/>.
        /// </summary>
        public InterleavedBuffer Data { get; set; }

        public int ItemSize { get; set; }

        public int Offset { get; set; }

        public bool Normalized { get; set; }

        public InterleavedBufferAttribute()
        {
        }

        public InterleavedBufferAttribute(InterleavedBuffer data, int itemSize, int offset, bool normalized = false)
        {
            Data = data;
            DataUuid = data?.Uuid ?? Guid.Empty;
            ItemSize = itemSize;
            Offset = offset;
            Normalized = normalized;
        }
    }
}
