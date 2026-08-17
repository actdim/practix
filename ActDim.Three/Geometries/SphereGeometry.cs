using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    [DataContract]
    public class SphereGeometryParameters
    {
        /// <summary>
        /// Sphere radius.
        /// </summary>
        [DataMember(Name = "radius")]
        public float Radius { get; set; }

        /// <summary>
        ///  Number of horizontal segments. Minimum value is 3.
        /// </summary>
        [DataMember(Name = "widthSegments")]
        public int WidthSegments { get; set; }

        /// <summary>
        /// Number of vertical segments. Minimum value is 2.
        /// </summary>
        [DataMember(Name = "heightSegments")]
        public int HeightSegments { get; set; }

        /// <summary>
        /// Specify horizontal starting angle (in radians).
        /// </summary>
        [DataMember(Name = "phiStart")]
        public float PhiStart { get; set; }

        /// <summary>
        /// Specify horizontal sweep angle size (in radians).
        /// </summary>
        [DataMember(Name = "phiLength")]
        public float PhiLength { get; set; }

        /// <summary>
        /// Specify horizontal sweep angle size (in radians).
        /// </summary>
        [DataMember(Name = "thetaStart")]
        public float ThetaStart { get; set; }

        /// <summary>
        /// Specify vertical sweep angle size (in radians).
        /// </summary>
        [DataMember(Name = "thetaLength")]
        public float ThetaLength { get; set; }

    }

    /// <summary>
    /// A class for generating sphere geometries.
    /// Analagous to: https://threejs.org/docs/index.html#api/geometries/SphereGeometry
    /// JS Source: https://github.com/mrdoob/three.js/blob/master/src/geometries/SphereGeometry.js
    /// </summary>
    [DataContract]
    public class SphereGeometry : Geometry
    {
        [DataMember(Name = "parameters")]
        public SphereGeometryParameters Parameters { get; set; }
    }

    [DataContract]
    public class SphereBufferGeometry : BufferGeometry
    {
        [DataMember(Name = "parameters")]
        public SphereGeometryParameters Parameters { get; set; }

        public SphereBufferGeometry(SphereGeometryParameters parameters)
        {
            Parameters = parameters;
        }
    }
}
