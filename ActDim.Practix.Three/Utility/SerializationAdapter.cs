using THREE.Core;
using System.Runtime.Serialization;

namespace THREE.Utility
{
    [DataContract]
    public abstract class SerializationAdapter
    {
        [DataMember(Order = 0)]
        public Metadata Metadata { get; set; }

		public SerializationAdapter()
        {
            Metadata = new Metadata
            {
                Version = 4.5,
                Generator = "ThreeLib"
            };
        }
    }

    [DataContract]
    public abstract class ObjectSerializationAdapter : SerializationAdapter
    {
        [DataMember(Order = 1)]
        public ElementCollection Geometries { get; set; }

        [DataMember(Order = 2)]
        public ElementCollection Images { get; set; }

        [DataMember(Order = 3)]
        public ElementCollection Textures { get; set; }

        [DataMember(Order = 4)]
        public ElementCollection Materials { get; set; }

        [DataMember(Order = 5)]
        public ElementCollection Fonts { get; set; }

		public ObjectSerializationAdapter()
        {
            Metadata.Type = "Object";
            Geometries = new ElementCollection();
            Materials = new ElementCollection();
            Fonts = new ElementCollection();
            Images = new ElementCollection();
            Textures = new ElementCollection();
        }

		public bool ShouldSerializeImages()
        {
            return Images.Count > 0;
        }

		public bool ShouldSerializeTextures()
        {
            return Textures.Count > 0;
        }

    }
}
