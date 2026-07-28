using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace THREE.Core
{
    /// <summary>
    /// Create a set of Shapes representing a font loaded in JSON format.
    /// Analagous to: https://threejs.org/docs/index.html#api/en/extras/core/Font
    /// </summary>
    [DataContract]
    public class Font : Element, IEquatable<Font>
    {
        /// <summary>
        /// String of text.
        /// </summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// (optional) Scale for the Shapes. Default is 100.
        /// </summary>
        [DataMember(Name = "size")]
        public float Size { get; set; }

        /// <summary>
        /// JSON data representing the font.
        /// </summary>
        [IgnoreDataMember]
        internal FontData FontData { get; set; }

        /// <summary>
        /// font data Uuid.
        /// </summary>
        [DataMember(Name = "data")]
        public Guid? Data {
            get {
                if (FontData != null)
                {
                    return FontData.Uuid;
                }
                else
                {
                    return null;
                }
            }
            set {
                if (FontData != null)
                {
                    FontData.Uuid = value.Value;
                }
            }
        }

        /// <summary>
        /// Used to check whether this or derived classes are fonts. Default is true.
        /// You should not change this, as it used internally by the renderer for optimisation.
        /// </summary>
        public bool IsFont { get; set; }

		public Font()
        {
            FontData = new FontData();
        }

        public virtual bool Equals(Font other)
        {
            if (other == null)
            {
                return false;
            }
            return FontData.Equals(other.FontData) && Size.Equals(other.Size);
        }
    }

    public class FontData : Element, IEquatable<FontData>
    {
		public object Data { get; set; }

		public virtual bool Equals(FontData other)
        {
            if (other == null)
            {
                return false;
            }

            // var name = ((JObject)Data).Property("familyName").Value.ToString();
            // var nameOther = ((JObject)other.Data).Property("familyName").Value.ToString();
            // return name == nameOther;
            return string.Equals(
                JsonConvert.SerializeObject(Data),
                JsonConvert.SerializeObject(other.Data),
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}