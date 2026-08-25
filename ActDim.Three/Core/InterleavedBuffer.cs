using System;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.Core
{
    /// <summary>
    /// Represents an interleaved buffer array for sharing data between multiple attributes.
    /// Analogous to https://threejs.org/docs/#api/en/core/InterleavedBuffer
    /// </summary>
    public class InterleavedBuffer : IElement
    {
        /// <inheritdoc />
        public Guid Uuid { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>
        /// The typed numeric array payload storing interleaved attributes.
        /// </summary>
        public ITypedArray Values { get; set; }

        /// <summary>
        /// The number of typed items per vertex stored in the buffer array.
        /// </summary>
        public int Stride { get; set; }

        /// <summary>
        /// Buffer usage hint.
        /// </summary>
        public int Usage { get; set; }

        public InterleavedBuffer()
        {
        }

        public InterleavedBuffer(ITypedArray values, int stride)
        {
            Values = values;
            Stride = stride;
        }
    }
}
