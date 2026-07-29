using System;
using System.Runtime.Serialization;

namespace THREE.Core
{
    /// <summary>
    /// A font used by TextGeometry. NOTE: this is a ThreeLib extension — the standard three.js
    /// Object/Scene format has no `fonts` pool (fonts are loaded separately via FontLoader).
    /// </summary>
    [DataContract]
    public class Font : Element
    {
        /// <summary>String of text.</summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>(optional) Scale for the Shapes. Default is 100.</summary>
        [DataMember(Name = "size")]
        public float Size { get; set; }

        /// <summary>JSON data representing the font.</summary>
        [IgnoreDataMember]
        internal FontData FontData { get; set; }

        /// <summary>Font data Uuid.</summary>
        [DataMember(Name = "data")]
        public Guid? Data
        {
            get => FontData?.Uuid;
            set
            {
                if (FontData != null && value.HasValue)
                {
                    FontData.Uuid = value.Value;
                }
            }
        }

        public bool IsFont { get; set; }

        public Font()
        {
            FontData = new FontData();
        }
    }

    public class FontData : Element
    {
        public object Data { get; set; }
    }
}
