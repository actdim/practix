using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    /// <summary>
    /// Similar to <see cref="ShaderMaterial"/>, but with no built-in Three.js attributes or uniforms automatically prepended to shaders.
    /// Analogous to https://threejs.org/docs/#api/en/materials/RawShaderMaterial
    /// </summary>
    [DataContract]
    public class RawShaderMaterial : ShaderMaterial
    {
        public RawShaderMaterial() : base()
        {
        }
    }
}
