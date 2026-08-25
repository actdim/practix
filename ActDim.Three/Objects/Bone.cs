using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// A bone which is part of a <see cref="Skeleton"/>.
    /// Analogous to https://threejs.org/docs/#api/en/objects/Bone
    /// </summary>
    [DataContract]
    public class Bone : Object3D
    {
        public Bone() : base()
        {
        }
    }
}
