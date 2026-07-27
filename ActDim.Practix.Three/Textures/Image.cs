using System;
using System.IO;
using System.Runtime.Serialization;
using THREE.Core;

namespace THREE.Textures
{
    public interface IImage : IElement
    {
    }

    [DataContract]
    public class Image : IImage, IEquatable<Image>
    {
        /// <summary>
        /// Object Id.
        /// </summary>
        [DataMember]
        public Guid Uuid { get; set; }

        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Image url. This can be the path to the image resource (.jpg, .png, etc), or a base64 encoded asset.
        /// </summary>
        [DataMember]
        public string Url { get; set; }

        /// <summary>
        /// Image path.
        /// </summary>
        [IgnoreDataMember]
        internal string OriginalPath { get; set; }

        /// <summary>
        /// Image exists flag.
        /// </summary>
        [IgnoreDataMember]
        internal bool Exists { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Image()
        {
            Uuid = Guid.NewGuid();
        }

        /// <summary>
        /// Encode image to base64.
        /// TODO: consider removing this to the example application.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetDataURL(string fileName)
        {
            return $"data:image/{Path.GetExtension(fileName).Replace(".", "")};base64,{Convert.ToBase64String(File.ReadAllBytes(fileName))}";
        }

		public override bool Equals(object other)
        {
            // return Equals(other as Image);
            if (other.GetType() == typeof(Image))
            {
                return Equals((Image)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }

		public bool Equals(Image other)
        {
            if (other == null)
            {
                return false;
            }
            return string.Equals(Url, other.Url);
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }

        /// <summary>
        /// Override the == operator.
        /// </summary>
        /// <param name="a">The first image.</param>
        /// <param name="b">The second image.</param>
        /// <returns>True if images are equal, false if not.</returns>
        public static bool operator ==(Image a, Image b)
        {
            var aIsNull = ReferenceEquals(a, null);
            var bIsNull = ReferenceEquals(b, null);
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
        /// <param name="a">The first image.</param>
        /// <param name="b">The second image.</param>
        /// <returns>False if images are equal, true if not.</returns>
        public static bool operator !=(Image a, Image b)
        {
            return !(a == b);
        }
    }
}
