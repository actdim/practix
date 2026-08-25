using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// A special version of <see cref="Mesh"/> with instanced rendering support.
    /// Analogous to https://threejs.org/docs/#api/en/objects/InstancedMesh
    /// </summary>
    [DataContract]
    public class InstancedMesh : Mesh
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "instanceMatrix")]
        public BufferAttribute InstanceMatrix { get; set; }

        [DataMember(Name = "instanceColor")]
        public BufferAttribute InstanceColor { get; set; }

        public InstancedMesh() : base()
        {
        }

        public InstancedMesh(IGeometry geometry, IMaterial material, int count) : base()
        {
            Geometry = geometry;
            Material = material;
            Count = count;
        }
    }
}
