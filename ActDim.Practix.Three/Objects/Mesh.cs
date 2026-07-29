using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using THREE.Core;
using THREE.Materials;

namespace THREE.Objects
{
    [DataContract]
    public class Mesh : Object3D, IGeometryContainer
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
        public Guid GeometryUuid => Geometry.Uuid;

        /// <summary>
        /// The material associated with this mesh.
        /// </summary>
        [IgnoreDataMember]
        public IMaterial Material { get; set; }

        /// <summary>
        /// Multiple materials, selected per geometry group by <c>materialIndex</c>. Takes precedence over
        /// <see cref="Material"/> when non-empty (multi-material mesh).
        /// </summary>
        [IgnoreDataMember]
        public IList<IMaterial> Materials { get; set; } = [];

        /// <summary>
        /// Serialized as a single material uuid, or an array of uuids for a multi-material mesh.
        /// </summary>
        [DataMember(Name = "material")]
        public object MaterialReference
        {
            get
            {
                if (Materials != null && Materials.Count > 0)
                {
                    return Materials.Select(material => material.Uuid).ToArray();
                }
                return Material?.Uuid;
            }
        }
    }
}
