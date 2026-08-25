using System;
using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Math;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// A mesh that has a <see cref="Skeleton"/> with bones that can then be used to animate the vertices.
    /// Analogous to https://threejs.org/docs/#api/en/objects/SkinnedMesh
    /// </summary>
    [DataContract]
    public class SkinnedMesh : Mesh
    {
        [DataMember(Name = "bindMode")]
        public string BindMode { get; set; } = "attached";

        [DataMember(Name = "bindMatrix")]
        public Matrix4 BindMatrix { get; set; }

        [IgnoreDataMember]
        public Skeleton Skeleton { get; set; }

        [DataMember(Name = "skeleton")]
        public Guid? SkeletonUuid => Skeleton?.Uuid;

        public SkinnedMesh() : base()
        {
        }

        public SkinnedMesh(IGeometry geometry, IMaterial material) : base(geometry, material)
        {
        }
    }
}
