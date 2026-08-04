using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ActDim.Three.Core
{
    [DataContract]
    public class BufferGeometry : Element, IGeometry
    {
        [DataMember(Name = "data")]
        public BufferGeometryData Data { get; set; }

        [IgnoreDataMember]
        public BufferGeometryBoundingSphere BoundingSphere
        {
            get { return Data.BoundingSphere; }
            set { Data.BoundingSphere = value; }
        }

        [IgnoreDataMember]
        public IDictionary<string, BufferAttribute> Attributes
        {
            get { return Data.Attributes; }
        }

        [IgnoreDataMember]
        public BufferAttribute Index
        {
            get { return Data.Index; }
            set { Data.Index = value; }
        }

        [IgnoreDataMember]
        public List<GeometryGroup> Groups
        {
            get { return Data.Groups; }
            set { Data.Groups = value; }
        }

        [IgnoreDataMember]
        public IDictionary<string, List<BufferAttribute>> MorphAttributes
        {
            get { return Data.MorphAttributes; }
        }

        [IgnoreDataMember]
        public DrawRange DrawRange
        {
            get { return Data.DrawRange; }
            set { Data.DrawRange = value; }
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
        public IDictionary<string, BufferAttribute> Attributes { get; set; }

        [DataMember(Name = "index")]
        public BufferAttribute Index { get; set; }

        /// <summary>
        /// Triangle ranges drawn with different materials (paired with a multi-material mesh).
        /// </summary>
        [DataMember(Name = "groups")]
        public List<GeometryGroup> Groups { get; set; }

        /// <summary>
        /// Morph targets / blend shapes: attribute name -> per-target buffers (e.g. "position").
        /// </summary>
        [DataMember(Name = "morphAttributes")]
        public IDictionary<string, List<BufferAttribute>> MorphAttributes { get; set; }

        /// <summary>
        /// Range of the index buffer to render. Omitted when the whole geometry is drawn.
        /// </summary>
        [DataMember(Name = "drawRange")]
        public DrawRange DrawRange { get; set; }

        /// <summary>
        /// Bounding sphere is client-computed and is intentionally NOT serialized (see §10 of the plan).
        /// </summary>
        [IgnoreDataMember]
        internal BufferGeometryBoundingSphere BoundingSphere { get; set; }

        public BufferGeometryData()
        {
            Attributes = new Dictionary<string, BufferAttribute>();
            Groups = [];
            MorphAttributes = new Dictionary<string, List<BufferAttribute>>();
        }

        public bool ShouldSerializeGroups()
        {
            return Groups != null && Groups.Count > 0;
        }

        public bool ShouldSerializeMorphAttributes()
        {
            return MorphAttributes != null && MorphAttributes.Count > 0;
        }
    }

    /// <summary>
    /// A range of the index buffer drawn with one material (three.js geometry group).
    /// </summary>
    [DataContract]
    public class GeometryGroup
    {
        [DataMember(Name = "start")]
        public int Start { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "materialIndex")]
        public int MaterialIndex { get; set; }
    }

    /// <summary>
    /// The portion of the index buffer to render (three.js drawRange).
    /// </summary>
    [DataContract]
    public class DrawRange
    {
        [DataMember(Name = "start")]
        public int Start { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }
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
}
