using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Math;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// Use an array of <see cref="Bone"/> objects to create a skeleton.
    /// Analogous to https://threejs.org/docs/#api/en/objects/Skeleton
    /// </summary>
    public class Skeleton : Element
    {
        [DataMember(Name = "bones")]
        public List<Guid> BoneUuids { get; set; } = new List<Guid>();

        [IgnoreDataMember]
        public List<Bone> Bones { get; set; } = new List<Bone>();

        [DataMember(Name = "boneInverses")]
        public List<Matrix4> BoneInverses { get; set; } = new List<Matrix4>();

        public Skeleton()
        {
        }
    }
}
