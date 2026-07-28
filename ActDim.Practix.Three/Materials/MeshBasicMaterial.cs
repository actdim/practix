using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using THREE.Textures;

namespace THREE.Materials
{
    [DataContract]
    public class MeshBasicMaterial : Material, IEquatable<MeshBasicMaterial>
    {
        /// <summary>
        /// Material diffuse color.
        /// </summary>
        [DataMember(Name = "color")]
        public int Color { get; set; }

        /// <summary>
        /// Material ao map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture AoMap { get; set; }

        /// <summary>
        /// ao Uuid.
        /// </summary>
        [DataMember(Name = "aoMap")]
        public Guid? AoMapUuid {
            get {
                if (AoMap != null)
                {
                    return AoMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Material diffuse map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture Map { get; set; }

        /// <summary>
        /// The Uuid of the diffuse map.
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

        /// <summary>
        /// Material alpha map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture AlphaMap { get; set; }

        /// <summary>
        /// AlphaMap Uuid.
        /// </summary>
        [DataMember(Name = "alphaMap")]
        public Guid? AlphaMapUuid {
            get {
                if (AlphaMap != null)
                {
                    return AlphaMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Material environment map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture EnvironmentMap { get; set; }

        /// <summary>
        /// Environment map Uuid.
        /// </summary>
        [DataMember(Name = "envMap")]
        public Guid? EnvironmentMapUuid {
            get {
                if (EnvironmentMap != null)
                {
                    return EnvironmentMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        internal Texture LightMap { get; set; }

        /// <summary>
        /// Light map Uuid.
        /// </summary>
        [DataMember(Name = "lightMap")]
        public Guid? LightMapUuid {
            get {
                if (LightMap != null)
                {
                    return LightMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        internal Texture SpecularMap { get; set; }

        /// <summary>
        /// Specular map Uuid.
        /// </summary>
        [DataMember(Name = "specularMap")]
        public Guid? SpecularMapUuid {
            get {
                if (SpecularMap != null)
                {
                    return SpecularMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        internal Dictionary<string, Texture> Textures { get; set; }

        /// <summary>
        /// Returns material textures as a dictionary.
        /// </summary>
        /// <returns>Dictionary with the texture type as the key. For example, "AlphaMap" key will have a Texture that contains an Alpha Map image.</returns>
        internal Dictionary<string, Texture> GetTextures()
        {
            return Textures;
        }

		public MeshBasicMaterial()
        {
            Textures = new Dictionary<string, Texture>();
        }

		public bool Equals(MeshBasicMaterial other)
        {
            if (other == null)
            {
                return false;
            }
            return Color.Equals(other.Color) &&
                   AoMap == other.AoMap &&
                   Map == other.Map &&
                   LightMap == other.LightMap &&
                   AlphaMap == other.AlphaMap &&
                   EnvironmentMap == other.EnvironmentMap;
        }

		public override bool Equals(Material other)
        {
            if (other.GetType() == typeof(MeshBasicMaterial))
            {
                return Equals((MeshBasicMaterial)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }
    }
}