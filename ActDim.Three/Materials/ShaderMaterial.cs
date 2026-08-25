using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Material rendered with custom GLSL shaders and uniforms.
    /// Analogous to https://threejs.org/docs/#api/en/materials/ShaderMaterial
    /// </summary>
    [DataContract]
    public class ShaderMaterial : Material
    {
        [DataMember(Name = "vertexShader")]
        public string VertexShader { get; set; }

        [DataMember(Name = "fragmentShader")]
        public string FragmentShader { get; set; }

        [DataMember(Name = "uniforms")]
        public Dictionary<string, object> Uniforms { get; set; } = new Dictionary<string, object>();

        [DataMember(Name = "wireframe")]
        public bool Wireframe { get; set; }

        public ShaderMaterial() : base()
        {
        }
    }
}
