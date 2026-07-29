using System;
using System.Runtime.Serialization;
using THREE.Textures;

namespace THREE.Materials
{
    [DataContract]
    public class MeshPhongMaterial : Material
    {
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
        /// Material bump map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture BumpMap { get; set; }

        /// <summary>
        /// BumpMap Uuid.
        /// </summary>
        [DataMember(Name = "bumpMap")]
        public Guid? BumpMapUuid {
            get {
                if (BumpMap != null)
                {
                    return BumpMap.Uuid;
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

    }
}
