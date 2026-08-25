using System.Runtime.Serialization;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.Textures
{
    /// <summary>
    /// Creates a texture directly from raw pixel data.
    /// Analogous to https://threejs.org/docs/#api/en/textures/DataTexture
    /// </summary>
    [DataContract]
    public class DataTexture : Texture
    {
        [DataMember(Name = "data")]
        public ITypedArray Data { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        public DataTexture() : base()
        {
        }

        public DataTexture(ITypedArray data, int width, int height) : this()
        {
            Data = data;
            Width = width;
            Height = height;
        }
    }
}
