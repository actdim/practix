using System;
using System.Runtime.Serialization;
using THREE.Core;

namespace THREE.Geometries
{
    [DataContract]
    public class SphereGeometryParameters : IEquatable<SphereGeometryParameters>
    {
        /// <summary>
        /// Sphere radius.
        /// </summary>
        [DataMember]
        public float Radius { get; set; }

        /// <summary>
        ///  Number of horizontal segments. Minimum value is 3.
        /// </summary>		
        [DataMember]
        public int WidthSegments { get; set; }

        /// <summary>
        /// Number of vertical segments. Minimum value is 2.
        /// </summary>		
        [DataMember]
        public int HeightSegments { get; set; }

        /// <summary>
        /// Specify horizontal starting angle (in radians).
        /// </summary>
        [DataMember]
        public float PhiStart { get; set; }

        /// <summary>
        /// Specify horizontal sweep angle size (in radians).
        /// </summary>		
        [DataMember]
        public float PhiLength { get; set; }

        /// <summary>
        /// Specify horizontal sweep angle size (in radians).
        /// </summary>
        [DataMember]
        public float ThetaStart { get; set; }

        /// <summary>
        /// Specify vertical sweep angle size (in radians).
        /// </summary>
        [DataMember]
        public float ThetaLength { get; set; }

		public bool Equals(SphereGeometryParameters other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Radius.Equals(other.Radius) &&
                        WidthSegments.Equals(other.WidthSegments) &&
                        HeightSegments.Equals(other.HeightSegments) &&
                        PhiStart.Equals(other.PhiStart) &&
                        PhiLength.Equals(other.PhiLength) &&
                        ThetaStart.Equals(other.ThetaStart) &&
                        ThetaLength.Equals(other.ThetaLength);
            }
        }
    }

	public interface ISphereGeometry
    {
        SphereGeometryParameters Parameters { get; set; }
    }

    /// <summary>
    /// A class for generating sphere geometries.
    /// Analagous to: https://threejs.org/docs/index.html#api/geometries/SphereGeometry
    /// JS Source: https://github.com/mrdoob/three.js/blob/master/src/geometries/SphereGeometry.js
    /// </summary>	
    [DataContract]
    public class SphereGeometry : Geometry, ISphereGeometry, IEquatable<SphereGeometry>
    {
        [DataMember]
        public SphereGeometryParameters Parameters { get; set; }

        /// <summary>
        /// Check if this is equal to another geometry of this type.
        /// </summary>
        /// <param name="other">Other geometry.</param>
        /// <returns>True if the geometries contain the same property values. False if otherwise.</returns>
        bool IEquatable<SphereGeometry>.Equals(SphereGeometry other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Parameters.Equals(other.Parameters);
            }
        }
    }

    [DataContract]
    public class SphereBufferGeometry : BufferGeometry, ISphereGeometry, IEquatable<SphereBufferGeometry>
    {
        [DataMember]
        public SphereGeometryParameters Parameters { get; set; }

		public SphereBufferGeometry(SphereGeometryParameters parameters)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Check if this is equal to another geometry of this type.
        /// </summary>
        /// <param name="other">Other geometry.</param>
        /// <returns>True if the geometries contain the same property values. False if otherwise.</returns>
        bool IEquatable<SphereBufferGeometry>.Equals(SphereBufferGeometry other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Parameters.Equals(other.Parameters);
            }
        }
    }
}
