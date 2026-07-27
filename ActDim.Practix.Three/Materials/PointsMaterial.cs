using System;
using System.Runtime.Serialization;
using THREE.Textures;

namespace THREE.Materials
{
    /// <summary>
    /// Analogous to https://github.com/mrdoob/three.js/blob/master/src/materials/PointsMaterial.js
    /// </summary>
    [DataContract]
    public class PointsMaterial : Material, IEquatable<PointsMaterial>
    {
        /// <summary>
        /// Material color.
        /// </summary>
        [DataMember]
        public int Color { get; set; }

        /// <summary>
        /// Point size.
        /// </summary>
        [DataMember]
        public double Size { get; set; }

        /// <summary>
        /// Size attenuation flag.
        /// </summary>
        [DataMember]
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

		public bool Equals(PointsMaterial other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Color.Equals(other.Color) &&
                       Size.Equals(other.Size) &&
                       SizeAttenuation.Equals(other.SizeAttenuation);
            }
        }

		public override bool Equals(Material other)
        {
            if (other.GetType() == typeof(PointsMaterial))
            {
                return Equals((PointsMaterial)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }
    }
}
