using System;
using System.Runtime.Serialization;
using ActDim.Three.Textures;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material implementing toon / cel shading.
    /// Analogous to https://threejs.org/docs/#api/en/materials/MeshToonMaterial
    /// </summary>
    [DataContract]
    public class MeshToonMaterial : Material
    {
        [DataMember(Name = "color")]
        public int Color { get; set; } = 0xffffff;

        [DataMember(Name = "map")]
        public Guid? MapUuid { get; set; }

        [IgnoreDataMember]
        public Texture Map { get; set; }

        [DataMember(Name = "gradientMap")]
        public Guid? GradientMapUuid { get; set; }

        [IgnoreDataMember]
        public Texture GradientMap { get; set; }

        public MeshToonMaterial() : base()
        {
        }
    }
}
