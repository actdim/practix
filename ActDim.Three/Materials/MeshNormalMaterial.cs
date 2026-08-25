using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material that maps the normal vectors to RGB colors.
    /// Analogous to https://threejs.org/docs/#api/en/materials/MeshNormalMaterial
    /// </summary>
    [DataContract]
    public class MeshNormalMaterial : Material
    {
        [DataMember(Name = "wireframe")]
        public bool Wireframe { get; set; }

        public MeshNormalMaterial() : base()
        {
        }
    }
}
