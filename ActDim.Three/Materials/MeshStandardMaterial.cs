using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ActDim.Three.Textures;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Analogous to: https://github.com/mrdoob/three.js/blob/dev/src/materials/MeshStandardMaterial.js
    /// TODO: Add roughness and metalness maps.
    /// </summary>
    [DataContract]
    public class MeshStandardMaterial : Material
    {
        // TODO: use map type constants

        /// <summary>
        /// Material diffuse color.
        /// </summary>
        [DataMember(Name = "color")]
        public int Color { get; set; }

        /// <summary>
        /// Material roughness.
        /// </summary>
        [DataMember(Name = "roughness")]
        public double Roughness { get; set; }

        /// <summary>
        /// Material metalness.
        /// </summary>
        [DataMember(Name = "metalness")]
        public double Metalness { get; set; }

        /// <summary>
        /// Material ambient color.
        /// </summary>
        [DataMember(Name = "ambient")]
        public int Ambient { get; set; }

        /// <summary>
        /// Material emissive color.
        /// </summary>
        [DataMember(Name = "emissive")]
        public int Emissive { get; set; }

        /// <summary>
        /// Material ao map.
        /// </summary>		
        [IgnoreDataMember]
        internal Texture AoMap {
            get {
                if (Textures.ContainsKey("AoMap"))
                {
                    return Textures["AoMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("AoMap"))
                {
                    Textures["AoMap"] = value;
                }
                else
                {
                    Textures.Add("AoMap", value);
                }
            }
        }

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
        /// Material alpha map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture AlphaMap {
            get {
                if (Textures.ContainsKey("AlphaMap"))
                {
                    return Textures["AlphaMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("AlphaMap"))
                {
                    Textures["AlphaMap"] = value;
                }
                else
                {
                    Textures.Add("AlphaMap", value);
                }
            }
        }

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
        /// Material bump map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture BumpMap {
            get {
                if (Textures.ContainsKey("BumpMap"))
                {
                    return Textures["BumpMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("BumpMap"))
                {
                    Textures["BumpMap"] = value;
                }
                else
                {
                    Textures.Add("BumpMap", value);
                }
            }
        }

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
        /// The displacement map affects the position of the mesh's vertices. Unlike other maps which only affect the light and shade of the material the displaced vertices can cast shadows, block other objects, and otherwise act as real geometry. The displacement texture is an image where the value of each pixel (white being the highest) is mapped against, and repositions, the vertices of the mesh.
        /// </summary>
        [IgnoreDataMember]
        internal Texture DisplacementMap {
            get {
                if (Textures.ContainsKey("DisplacementMap"))
                {
                    return Textures["DisplacementMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("DisplacementMap"))
                {
                    Textures["DisplacementMap"] = value;
                }
                else
                {
                    Textures.Add("DisplacementMap", value);
                }
            }
        }

        /// <summary>
        /// Displacement map Uuid.
        /// </summary>
        [DataMember(Name = "displacementMap")]
        public Guid? DisplacementMapUuid {
            get {
                if (DisplacementMap != null)
                {
                    return DisplacementMap.Uuid;
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
        internal Texture EnvironmentMap {
            get {
                if (Textures.ContainsKey("EnvironmentMap"))
                {
                    return Textures["EnvironmentMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("EnvironmentMap"))
                {
                    Textures["EnvironmentMap"] = value;
                }
                else
                {
                    Textures.Add("EnvironmentMap", value);
                }
            }
        }

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

        /// <summary>
        /// Set emisssive (glow) map. The emissive map color is modulated by the emissive color and the emissive intensity. If you have an emissive map, be sure to set the emissive color to something other than black.
        /// </summary>
        [IgnoreDataMember]
        internal Texture EmissiveMap {
            get {
                if (Textures.ContainsKey("EmissiveMap"))
                {
                    return Textures["EmissiveMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("EmissiveMap"))
                {
                    Textures["EmissiveMap"] = value;
                }
                else
                {
                    Textures.Add("EmissiveMap", value);
                }
            }
        }

        /// <summary>
        /// Emissive map Uuid.
        /// </summary>
        [DataMember(Name = "emissiveMap")]
        public Guid? EmissiveMapUuid {
            get {
                if (EmissiveMap != null)
                {
                    return EmissiveMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        internal Texture LightMap {
            get {
                if (Textures.ContainsKey("LightMap"))
                {
                    return Textures["LightMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("LightMap"))
                {
                    Textures["LightMap"] = value;
                }
                else
                {
                    Textures.Add("LightMap", value);
                }
            }
        }

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

        /// <summary>
        /// Material diffuse map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture Map {
            get {
                if (Textures.ContainsKey("Map"))
                {
                    return Textures["Map"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("Map"))
                {
                    Textures["Map"] = value;
                }
                else
                {
                    Textures.Add("Map", value);
                }
            }
        }

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
        /// Material metalness map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture MetalnessMap {
            get {
                if (Textures.ContainsKey("MetalnessMap"))
                {
                    return Textures["MetalnessMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("MetalnessMap"))
                {
                    Textures["MetalnessMap"] = value;
                }
                else
                {
                    Textures.Add("MetalnessMap", value);
                }
            }
        }

        /// <summary>
        /// The Uuid of the metalness map.
        /// </summary>
        [DataMember(Name = "metalnessMap")]
        public Guid? MetalnessMapUuid {
            get {
                if (MetalnessMap != null)
                {
                    return MetalnessMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Material normal map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture NormalMap {
            get {
                if (Textures.ContainsKey("NormalMap"))
                {
                    return Textures["NormalMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("NormalMap"))
                {
                    Textures["NormalMap"] = value;
                }
                else
                {
                    Textures.Add("NormalMap", value);
                }
            }
        }

        /// <summary>
        /// The Uuid of the normal map.
        /// </summary>
        [DataMember(Name = "normalMap")]
        public Guid? NormalMapUuid {
            get {
                if (NormalMap != null)
                {
                    return NormalMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Material roughness map.
        /// </summary>
        [IgnoreDataMember]
        internal Texture RoughnessMap {
            get {
                if (Textures.ContainsKey("RoughnessMap"))
                {
                    return Textures["RoughnessMap"];
                }
                else
                {
                    return null;
                }
            }
            set {
                if (Textures.ContainsKey("RoughnessMap"))
                {
                    Textures["RoughnessMap"] = value;
                }
                else
                {
                    Textures.Add("RoughnessMap", value);
                }
            }
        }

        /// <summary>
        /// The Uuid of the roughness map.
        /// </summary>
        [DataMember(Name = "roughnessMap")]
        public Guid? RoughnessMapUuid {
            get {
                if (RoughnessMap != null)
                {
                    return RoughnessMap.Uuid;
                }
                else
                {
                    return null;
                }
            }
        }

		[IgnoreDataMember]
        internal Dictionary<string, Texture> Textures { get; set; }

        public MeshStandardMaterial()
        {
            Textures = new Dictionary<string, Texture>();
        }

        /// <summary>
        /// Creates a MeshStandardMaterial with some default settings.
        /// </summary>
        /// <returns>An MeshStandardMaterial with default values.</returns>
        public static MeshStandardMaterial Default()
        {
            return new MeshStandardMaterial()
            {
                Color = 16777215,
                Roughness = 1.00,
                Metalness = 0.25,
                Transparent = false,
                Opacity = 1,
                Emissive = 0
            };
        }

        /// <summary>
        /// Returns material textures as a dictionary.
        /// </summary>
        /// <returns>Dictionary with the texture type as the key. For example, "AlphaMap" key will have a Texture that contains an Alpha Map image.</returns>
        internal Dictionary<string, Texture> GetTextures()
        {
            return Textures;
        }

    }
}