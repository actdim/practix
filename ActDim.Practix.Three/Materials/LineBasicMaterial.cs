using System;
using System.Runtime.Serialization;

namespace THREE.Materials
{
    [DataContract]
    public class LineBasicMaterial : Material, IEquatable<LineBasicMaterial>
    {
        /// <summary>
        /// The material color.
        /// </summary>
        [DataMember]
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

		public bool Equals(LineBasicMaterial other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Color.Equals(other.Color) &&
                       LineWidth.Equals(other.LineWidth); //&&
                                                          //LineJoin.Equals(other.LineJoin) &&
                                                          //LineCap.Equals(other.LineCap);
            }
        }

		public override bool Equals(Material other)
        {
            if (other.GetType() == typeof(LineBasicMaterial))
            {
                return Equals((LineBasicMaterial)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }
    }
}
