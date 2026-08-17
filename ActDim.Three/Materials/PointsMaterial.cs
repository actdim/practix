using System;
using System.Runtime.Serialization;
using ActDim.Three.Textures;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Analogous to https://github.com/mrdoob/three.js/blob/master/src/materials/PointsMaterial.js
    /// </summary>
    [DataContract]
    public class PointsMaterial : Material
    {
        /// <summary>
        /// Material color.
        /// </summary>
        [DataMember(Name = "color")]
        public int Color { get; set; }

        /// <summary>
        /// Point size.
        /// </summary>
        [DataMember(Name = "size")]
        public double Size { get; set; }

        /// <summary>
        /// Size attenuation flag.
        /// </summary>
        [DataMember(Name = "sizeAttenuation")]
        public bool SizeAttenuation { get; set; }

        /// <summary>
        /// The diffuse map texture.
        /// </summary>
        [IgnoreDataMember]
        internal Texture Map { get; set; }

        /// <summary>
        /// Material diffuse color map.
        /// </summary>
        [DataMember(Name = "map")]
        public Guid? MapUuid {
            get {
                if (Map != null)
                {
                    return Map.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

    }
}
