using System.Collections.Generic;
using System.Runtime.Serialization;

namespace THREE.Core
{
    [DataContract]
    public class BufferGeometry : Element, IGeometry
    {
		[DataMember(Name = "data")]
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

    }

    [DataContract]
    public class BufferGeometryData
    {
		[DataMember(Name = "attributes")]
        public IDictionary<string, BufferAttribute> Attributes { get; private set; }

		[DataMember(Name = "index")]
        public BufferAttribute Index { get; set; }

		[DataMember(Name = "boundingSphere")]
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
        [DataMember(Name = "center")]
        public float[] Center { get; set; }

        /// <summary>
        /// Radius of the bounding sphere.
        /// </summary>
        [DataMember(Name = "radius")]
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
