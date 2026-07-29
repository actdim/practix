using System.Runtime.Serialization;
using THREE.Core;

namespace THREE.Geometries
{
    [DataContract]
    public class TextGeometryParameters
    {
        /// <summary>
        /// An instance of THREE.Font.
        /// </summary>
        [DataMember(Name = "font")]
        public Font Font { get; set; }

        /// <summary>
        /// Size of the text. Default is 100.
        /// </summary>
        [DataMember(Name = "size")]
        public float Size { get; set; }

        /// <summary>
        /// Thickness to extrude text. Default is 50.
        /// </summary>
        [DataMember(Name = "height")]
        public float Height { get; set; }

        /// <summary>
        /// Number of points on the curves. Default is 12.
        /// </summary>
        [DataMember(Name = "curveSegments")]
        public int CurveSegments { get; set; }

        /// <summary>
        /// Turn on bevel. Default is False.
        /// </summary>
        [DataMember(Name = "bevelEnabled")]
        public bool BevelEnabled { get; set; }

        /// <summary>
        /// How deep into text bevel goes. Default is 10.
        /// </summary>
        [DataMember(Name = "bevelThickness")]
        public float BevelThickness { get; set; }

        /// <summary>
        /// How far from text outline is bevel. Default is 8.
        /// </summary>
        [DataMember(Name = "bevelSize")]
        public float BevelSize { get; set; }

        /// <summary>
        /// Number of bevel segments. Default is 3.
        /// </summary>
        [DataMember(Name = "bevelSegments")]
        public int BevelSegments { get; set; }

    }

    /// <summary>
    /// A class for generating text geometries.
    /// Analagous to: https://threejs.org/docs/index.html#api/en/geometries/TextGeometry
    /// JS Source: https://github.com/mrdoob/three.js/blob/master/src/geometries/TextGeometry.js
    /// </summary>
    [DataContract]
    public class TextGeometry : Geometry
    {
        [DataMember(Name = "parameters")]
        public TextGeometryParameters Parameters { get; set; }
    }

    [DataContract]
    public class TextBufferGeometry : Geometry
    {
        [DataMember(Name = "parameters")]
        public TextGeometryParameters Parameters { get; set; }

        public TextBufferGeometry(TextGeometryParameters parameters)
        {
            Parameters = parameters;
        }
    }
}