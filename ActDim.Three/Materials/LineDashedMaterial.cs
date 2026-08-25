using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material for drawing wireframe-style geometries with dashed lines.
    /// Analogous to https://threejs.org/docs/#api/en/materials/LineDashedMaterial
    /// </summary>
    [DataContract]
    public class LineDashedMaterial : Material
    {
        [DataMember(Name = "color")]
        public int Color { get; set; } = 0xffffff;

        [DataMember(Name = "scale")]
        public double Scale { get; set; } = 1.0;

        [DataMember(Name = "dashSize")]
        public double DashSize { get; set; } = 3.0;

        [DataMember(Name = "gapSize")]
        public double GapSize { get; set; } = 1.0;

        [DataMember(Name = "linewidth")]
        public double Linewidth { get; set; } = 1.0;

        public LineDashedMaterial() : base()
        {
        }
    }
}
