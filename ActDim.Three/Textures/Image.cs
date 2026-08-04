using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Textures
{
    /// <summary>
    /// An image resource referenced by textures. In the three.js format an image entry is { uuid, url }.
    /// </summary>
    [DataContract]
    public class Image : Element
    {
        /// <summary>
        /// Image url. This can be a path to the image resource (.jpg, .png, …) or a base64-encoded asset.
        /// </summary>
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
