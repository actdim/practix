using System;
using System.Linq;
using System.Runtime.Serialization;
using THREE.Core;
using THREE.Utility;

namespace THREE.Textures
{
    public interface ITexture : IElement
    {
    }

    [DataContract]
    public class Texture : ITexture, IEquatable<Texture>
    {

        /// <summary>
        /// Object Id.
        /// </summary>
        [DataMember(Name = "uuid")]
        public Guid Uuid { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        // public int Type { get; set; }
        // add texture enums
        // see https://threejs.org/docs/#api/en/constants/Textures

        /// <summary>
        /// Image associated with this texture.
        /// </summary>
        [IgnoreDataMember]
        internal Image Image { get; set; }

        /// <summary>
        /// URL of the image.
        /// </summary>
        [DataMember(Name = "image")]
        public Guid? ImageUuid {
            get {
                if (Image != null)
                {
                    return Image.Uuid;
                }
                else
                {
                    return null;
                }
            }

        }

        /// <summary>
        /// Texture mapping.
        /// </summary>
        [DataMember(Name = "mapping")]
        public int Mapping { get; set; }

        /// <summary>
        /// Texture wrapping.
        /// </summary>
        [DataMember(Name = "wrap")]
        public int[] Wrap { get; set; }

        /// <summary>
        /// Texture repetition.
        /// </summary>
        [DataMember(Name = "repeat")]
        public float[] Repeat { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Texture()
        {
            Uuid = Guid.NewGuid();
        }

        public bool Equals(Texture other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Mapping.Equals(other.Mapping) &&
                        Repeat.SequenceEqual(other.Repeat) &&
                        Wrap.SequenceEqual(other.Wrap) &&
                        Image.Equals(other.Image);
            }
        }

        public override int GetHashCode()
        {
            return Utilities.CombineHashCodes(Mapping, Repeat, Wrap, Image);
        }

        public override bool Equals(object other)
        {
            //return Equals(other as Texture);
            if (other.GetType() == typeof(Texture))
            {
                return Equals((Texture)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Override the == operator.
        /// </summary>
        /// <param name="a">The first texture.</param>
        /// <param name="b">The second texture.</param>
        /// <returns>True if textures are equal, false if not.</returns>
        public static bool operator ==(Texture a, Texture b)
        {
            bool aIsNull = ReferenceEquals(a, null);
            bool bIsNull = ReferenceEquals(b, null);
            if (aIsNull & bIsNull)
            {
                return true;
            }
            if (aIsNull)
            {
                return false;
            }
            if (bIsNull)
            {
                return false;
            }
            return a.Equals(b);
        }

        /// <summary>
        /// Override the != operator.
        /// </summary>
        /// <param name="a">The first texture.</param>
        /// <param name="b">The second texture.</param>
        /// <returns>False if textures are equal, true if not.</returns>
        public static bool operator !=(Texture a, Texture b)
        {
            return !(a == b);
        }

    }
}
