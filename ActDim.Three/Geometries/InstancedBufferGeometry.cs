using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    /// <summary>
    /// An instanced version of <see cref="BufferGeometry"/>.
    /// Analogous to https://threejs.org/docs/#api/en/core/InstancedBufferGeometry
    /// </summary>
    [DataContract]
    public class InstancedBufferGeometry : BufferGeometry
    {
        /// <summary>
        /// Number of instances to render.
        /// </summary>
        [DataMember(Name = "instanceCount")]
        public int? InstanceCount { get; set; }

        public InstancedBufferGeometry() : base()
        {
        }
    }
}
