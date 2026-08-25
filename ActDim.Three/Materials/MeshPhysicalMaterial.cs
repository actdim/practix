using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// An extension of <see cref="MeshStandardMaterial"/>, providing more advanced Physically-Based Rendering (PBR) properties.
    /// Analogous to https://threejs.org/docs/#api/en/materials/MeshPhysicalMaterial
    /// </summary>
    [DataContract]
    public class MeshPhysicalMaterial : MeshStandardMaterial
    {
        [DataMember(Name = "clearcoat")]
        public double Clearcoat { get; set; } = 0.0;

        [DataMember(Name = "clearcoatRoughness")]
        public double ClearcoatRoughness { get; set; } = 0.0;

        [DataMember(Name = "transmission")]
        public double Transmission { get; set; } = 0.0;

        [DataMember(Name = "thickness")]
        public double Thickness { get; set; } = 0.0;

        [DataMember(Name = "ior")]
        public double Ior { get; set; } = 1.5;

        [DataMember(Name = "sheen")]
        public double Sheen { get; set; } = 0.0;

        [DataMember(Name = "iridescence")]
        public double Iridescence { get; set; } = 0.0;

        public MeshPhysicalMaterial() : base()
        {
        }
    }
}
