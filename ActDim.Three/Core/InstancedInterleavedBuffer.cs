using System;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.Core
{
    /// <summary>
    /// An instanced version of <see cref="InterleavedBuffer"/>.
    /// Analogous to https://threejs.org/docs/#api/en/core/InstancedInterleavedBuffer
    /// </summary>
    public class InstancedInterleavedBuffer : InterleavedBuffer
    {
        /// <summary>
        /// Defines how often a value of this buffer is repeated across instances. Default is 1.
        /// </summary>
        public int MeshPerAttribute { get; set; } = 1;

        public InstancedInterleavedBuffer() : base()
        {
        }

        public InstancedInterleavedBuffer(ITypedArray values, int stride, int meshPerAttribute = 1) : base(values, stride)
        {
            MeshPerAttribute = meshPerAttribute;
        }
    }
}
