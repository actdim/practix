using System.Runtime.Serialization;

namespace ActDim.Three.Materials
{
    [DataContract]
    public class LineBasicMaterial : Material
    {
        /// <summary>
        /// The material color.
        /// </summary>
        [DataMember(Name = "color")]
        public int Color { get; set; }

        /// <summary>
        /// The curve linewidth.
        /// </summary>
        [DataMember(Name = "linewidth")]
        public float LineWidth { get; set; }

        /// <summary>
        /// The type of capping for the line.
        /// </summary>
        [DataMember(Name = "linecap")]
        public string LineCap { get; set; }

        [DataMember(Name = "linejoin")]
        public string LineJoin { get; set; }

    }
}
