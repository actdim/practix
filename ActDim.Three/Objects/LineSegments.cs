using System;
using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;

namespace ActDim.Three.Objects
{
    [DataContract]
    public class LineSegments : Object3D, IGeometryContainer
    {
        /// <summary>
        /// The geometry associated with this Mesh.
        /// </summary>
        [IgnoreDataMember]
        public IGeometry Geometry { get; set; }

        /// <summary>
        /// Uuid of this geometry.
        /// </summary>
        [DataMember(Name = "geometry")]
        public Guid GeometryUuid { get { return Geometry.Uuid; } }

        /// <summary>
        /// The material associated with this mesh.
        /// </summary>
        [IgnoreDataMember]
        public IMaterial Material { get; set; }

        /// <summary>
        /// This object's material Uuid.
        /// </summary>
        [DataMember(Name = "material")]
        public Guid MaterialUuid { get { return (Material as Material).Uuid; } }

        public LineSegments()
        {
            Type = GetType().Name;
        }
    }
}