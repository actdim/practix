using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using THREE.Utility;

namespace THREE.Core
{
    [DataContract]
    public class BufferGeometry : Element, IGeometry, IEquatable<BufferGeometry>
    {
		[DataMember]
        public BufferGeometryData Data { get; set; }

        [IgnoreDataMember]
        public BufferGeometryBoundingSphere BoundingSphere {
            get { return Data.BoundingSphere; }
            set { Data.BoundingSphere = value; }
        }

        [IgnoreDataMember]
        public IDictionary<string, BufferAttribute> Attributes {
            get { return Data.Attributes; }
        }

        [IgnoreDataMember]
        public BufferAttribute Index {
            get { return Data.Index; }
            set { Data.Index = value; }
        }

        public BufferGeometry()
        {
            Data = new BufferGeometryData();
        }

        /// <summary>
        /// Convert this BufferGeometry to json format.
        /// </summary>

        /// <returns>The geometry as json.</returns>
        public override byte[] ToJSON() // bool format
        {
            return Utilities.Serialize(this);
        }

		public bool Equals(BufferGeometry other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Data.Attributes.SequenceEqual(other.Data.Attributes) &&
                       Data.BoundingSphere.Equals(other.BoundingSphere);
            }
        }

		public override bool Equals(object other)
        {
            // return Equals(other as BufferGeometry);
            if (other.GetType() == typeof(BufferGeometry))
            {
                return Equals((BufferGeometry)other) && base.Equals(other);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Override of the == operator.
        /// </summary>
        /// <param name="a">The first buffer geometry.</param>
        /// <param name="b">The second buffer geometry.</param>
        /// <returns>True if buffer geometries are equal, false if not.</returns>
        public static bool operator ==(BufferGeometry a, BufferGeometry b)
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
        /// <param name="a">The first buffer geometry.</param>
        /// <param name="b">The second buffer geometry.</param>
        /// <returns>False if buffer geometries are equal, true if not.</returns>
        public static bool operator !=(BufferGeometry a, BufferGeometry b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Override of the GetHashCode function.
        /// </summary>
        /// <returns>A hashcode of the combined data.</returns>
        public override int GetHashCode()
        {
            return Utilities.CombineHashCodes(Data.Attributes, Data.BoundingSphere);
        }
    }

    [DataContract]
    public class BufferGeometryData
    {
		[DataMember]
        public IDictionary<string, BufferAttribute> Attributes { get; private set; }

		[DataMember]
        public BufferAttribute Index { get; set; }

		[DataMember]
        internal BufferGeometryBoundingSphere BoundingSphere { get; set; }

		public BufferGeometryData()
        {
            Attributes = new Dictionary<string, BufferAttribute>();
        }
    }

    /// <summary>
    /// Data for the bounding sphere.
    /// </summary>
    public class BufferGeometryBoundingSphere
    {
        /// <summary>
        /// Center position of the bounding sphere.
        /// </summary>
        [DataMember]
        public float[] Center { get; set; }

        /// <summary>
        /// Radius of the bounding sphere.
        /// </summary>
        [DataMember]
        public float Radius { get; set; }

    }

    // public class BufferGeometrySerializationAdapter : SerializationAdapter
    // {
    // 	/// <summary>
    // 	/// Geometry data.
    // 	/// </summary>
    // 	[DataMember(Order = 1)]
    // 	public BufferGeometryData Data { get; set; }       
    // 	public BufferGeometrySerializationAdapter()
    // 	{
    // 		Metadata = new Metadata
    // 		{
    // 			Type = "BufferGeometry",
    // 			//Version = 4.5, //3
    // 			Generator = "ThreeLib-BufferGeometry.toJSON"
    // 		};       
    // 		Data = new BufferGeometryData();
    // 	}
    // }
}
