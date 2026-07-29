using System;
using System.Runtime.Serialization;
using THREE.Core;

namespace THREE.Textures
{
    [DataContract]
    public class Texture : Element
    {
        /// <summary>Image associated with this texture (referenced by uuid in JSON).</summary>
        [IgnoreDataMember]
        internal Image Image { get; set; }

        [DataMember(Name = "image")]
        public Guid? ImageUuid => Image?.Uuid;

        /// <summary>Texture mapping.</summary>
        [DataMember(Name = "mapping")]
        public int Mapping { get; set; }

        /// <summary>Texture wrapping.</summary>
        [DataMember(Name = "wrap")]
        public int[] Wrap { get; set; }

        /// <summary>Texture repetition.</summary>
        [DataMember(Name = "repeat")]
        public float[] Repeat { get; set; }
    }
}
