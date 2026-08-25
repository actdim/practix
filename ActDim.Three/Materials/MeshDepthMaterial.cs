using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material for drawing geometry by depth.
    /// Analogous to https://threejs.org/docs/#api/en/materials/MeshDepthMaterial
    /// </summary>
    [DataContract]
    public class MeshDepthMaterial : Material
    {
        [DataMember(Name = "wireframe")]
        public bool Wireframe { get; set; }

        public MeshDepthMaterial() : base()
        {
        }
    }
}
