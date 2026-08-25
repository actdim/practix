using System;
using System.Runtime.Serialization;
using ActDim.Three.Textures;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material for 2D billboard sprite rendering.
    /// Analogous to https://threejs.org/docs/#api/en/materials/SpriteMaterial
    /// </summary>
    [DataContract]
    public class SpriteMaterial : Material
    {
        [DataMember(Name = "color")]
        public int Color { get; set; } = 0xffffff;

        [DataMember(Name = "map")]
        public Guid? MapUuid { get; set; }

        [IgnoreDataMember]
        public Texture Map { get; set; }

        [DataMember(Name = "rotation")]
        public double Rotation { get; set; } = 0.0;

        [DataMember(Name = "sizeAttenuation")]
        public bool SizeAttenuation { get; set; } = true;

        public SpriteMaterial() : base()
        {
        }
    }
}
