using System.Runtime.Serialization;

namespace ActDim.Three.Textures
{
    /// <summary>
    /// Creates a texture based on data in compressed form.
    /// Analogous to https://threejs.org/docs/#api/en/textures/CompressedTexture
    /// </summary>
    [DataContract]
    public class CompressedTexture : Texture
    {
        [DataMember(Name = "format")]
        public int Format { get; set; }

        public CompressedTexture() : base()
        {
        }
    }
}
