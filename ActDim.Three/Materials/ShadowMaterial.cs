using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material that receives shadows but is otherwise transparent.
    /// Analogous to https://threejs.org/docs/#api/en/materials/ShadowMaterial
    /// </summary>
    [DataContract]
    public class ShadowMaterial : Material
    {
        [DataMember(Name = "color")]
        public int Color { get; set; } = 0x000000;

        public ShadowMaterial() : base()
        {
            Transparent = true;
        }
    }
}
