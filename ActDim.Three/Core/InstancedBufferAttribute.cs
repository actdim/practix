using System;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.Core
{
    /// <summary>
    /// An instanced attribute for instanced rendering. Analogous to https://threejs.org/docs/#api/en/core/InstancedBufferAttribute
    /// </summary>
    public class InstancedBufferAttribute : BufferAttribute
    {
        /// <summary>
        /// Defines how often a value of this buffer attribute is repeated across instances. Default is 1.
        /// </summary>
        public int MeshPerAttribute { get; set; } = 1;

        public InstancedBufferAttribute() : base()
        {
        }

        public InstancedBufferAttribute(string type, Array source, int itemSize, int meshPerAttribute = 1, bool normalized = false)
            : base(type, source, itemSize, normalized)
        {
            MeshPerAttribute = meshPerAttribute;
        }

        public static InstancedBufferAttribute Float32(float[] data, int itemSize, int meshPerAttribute = 1, bool normalized = false)
        {
            return new InstancedBufferAttribute
            {
                ItemSize = itemSize,
                Normalized = normalized,
                MeshPerAttribute = meshPerAttribute,
                Values = new Float32Array { Data = data },
            };
        }

        public static InstancedBufferAttribute Uint32(uint[] data, int itemSize, int meshPerAttribute = 1, bool normalized = false)
        {
            return new InstancedBufferAttribute
            {
                ItemSize = itemSize,
                Normalized = normalized,
                MeshPerAttribute = meshPerAttribute,
                Values = new Uint32Array { Data = data },
            };
        }
    }
}
