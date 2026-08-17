using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ActDim.Three.Core
{
    /// <summary>
    /// Base class for all geometries. \n
    /// Analogous to https://threejs.org/docs/index.html#api/core/Geometry \n
    /// Design based on need for Three.js Loaders.
    /// </summary>
    [DataContract]
    public class Geometry : Element, IGeometry
    {
        /// <summary>
        /// Geometry data.
        /// </summary>
        [DataMember(Name = "data")]
        public GeometryData Data { get; set; }

        /// <summary>
        /// List of vertices for this geometry.
        /// </summary>
        [IgnoreDataMember]
        public List<float> Vertices {
            get { return Data.RawVertices; }
            set { Data.RawVertices = value; }
        }

        /// <summary>
        /// List of colors for this geometry.
        /// </summary>
        [IgnoreDataMember]
        public List<int> Colors {
            get { return Data.Colors; }
            set { Data.Colors = value; }
        }

        /// <summary>
        /// List of faces for this geometry.
        /// </summary>
        [IgnoreDataMember]
        public List<int> Faces {
            get { return Data.Faces; }
            set { Data.Faces = value; }
        }

        /// <summary>
        /// List of normals for this geometry.
        /// </summary>
        [IgnoreDataMember]
        public List<float> Normals {
            get { return Data.RawNormals; }
            set { Data.RawNormals = value; }
        }

        /// <summary>
        /// The list of UVs associated with this geometry.
        /// </summary>
        [IgnoreDataMember]
        public List<List<float>> Uvs {
            get { return Data.Uvs; }
            set { Data.Uvs = value; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Geometry()
        {
            Type = GetType().Name;
            Data = new GeometryData();
        }

        /// <summary>
        /// Constructor with default values = null.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faces"></param>
        /// <param name="normals"></param>
        /// <param name="colors"></param>
        /// <param name="uvs"></param>
        public Geometry(List<float> vertices = null, List<int> faces = null, List<float> normals = null, List<int> colors = null, List<List<float>> uvs = null) : this()
        {
            if (vertices == null)
            {
                return;
            }

            Vertices = vertices;

            if (normals != null && normals.Count > 0)
            {
                Normals = normals;
            }

            if (colors != null && colors.Count > 0)
            {
                Colors = colors;
            }

            if (uvs != null && uvs.Count > 0)
            {
                Uvs = uvs;
            }

            if (faces != null)
            {
                Faces = faces;
            }
        }

        /// <summary>
        /// Utility method for processing faces.
        /// TODO: Extend for all types of faces and switches.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="vertexColors"></param>
        /// <param name="uvs"></param>
        /// <returns>A list of int.</returns>
        public static List<int> ProcessFaceArray(IEnumerable<int[]> faces, bool vertexColors, bool uvs)
        {
            var face = new GeometryFace
            {
                Topology = false,
                VertexColors = vertexColors,
                FaceColor = false,
                FaceMaterial = false,
                FaceNormals = false,
                FaceUVs = false,
                FaceVertexUVs = uvs,
                VertexNormals = true
            };

            List<int> facesIndex = [];

            if (faces != null)
            {
                foreach (var meshFace in faces)
                {
                    if (meshFace.Length == 3) // has count 3
                    {
                        face.Topology = false;

                        facesIndex.Add(face.GetFaceType());

                        facesIndex.Add(meshFace[0]); //A
                        facesIndex.Add(meshFace[1]); //B
                        facesIndex.Add(meshFace[2]); //C

                        if (face.VertexNormals)
                        {
                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                        }

                        if (face.VertexColors)
                        {
                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                        }

                        if (face.FaceVertexUVs)
                        {
                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                        }
                    }
                    else
                    {
                        face.Topology = true;

                        facesIndex.Add(face.GetFaceType());

                        facesIndex.Add(meshFace[0]); //A
                        facesIndex.Add(meshFace[1]); //B
                        facesIndex.Add(meshFace[2]); //C
                        facesIndex.Add(meshFace[3]); //D

                        if (face.VertexNormals)
                        {

                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                            facesIndex.Add(meshFace[3]); //D
                        }

                        if (face.VertexColors)
                        {
                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                            facesIndex.Add(meshFace[3]); //D
                        }

                        if (face.FaceVertexUVs)
                        {
                            facesIndex.Add(meshFace[0]); //A
                            facesIndex.Add(meshFace[1]); //B
                            facesIndex.Add(meshFace[2]); //C
                            facesIndex.Add(meshFace[3]); //D
                        }
                    }
                }
            }

            return facesIndex;
        }

        /// <summary>
        /// Utility method for flattening a List of float[].
        /// </summary>
        /// <param name="vertices">The list to flatten.</param>
        /// <returns>A list of float.</returns>
        public static List<float> ProcessVertexArray(IEnumerable<float[]> vertices)
        {
            var Vertices = new List<float>();

            foreach (var vert in vertices)
            {
                Vertices.Add(vert[0]);
                Vertices.Add(vert[1]);
                Vertices.Add(vert[2]);
            }

            return Vertices;
        }

        /// <summary>
        /// Flatten a List of float[].
        /// </summary>
        /// <param name="normals">The list to flatten.</param>
        /// <returns>A list of float.</returns>
        public static List<float> ProcessNormalArray(IEnumerable<float[]> normals)
        {
            var Normals = new List<float>();

            foreach (var norm in normals)
            {
                Normals.Add(norm[0]);
                Normals.Add(norm[1]);
                Normals.Add(norm[2]);
            }

            return Normals;
        }

    }

    [DataContract]
    public class GeometryData
    {
        [IgnoreDataMember]
        internal List<float> RawVertices { get; set; }

        [DataMember(Name = "vertices")]
        public List<float> Vertices { get => RawVertices; set => RawVertices = value; }

        [DataMember(Name = "colors")]
        public List<int> Colors { get; set; }

        [DataMember(Name = "faces")]
        public List<int> Faces { get; set; }

        /// <summary>
        /// The list of UVs associated with this geometry.
        /// </summary>
        [DataMember(Name = "uvs")]
        public List<List<float>> Uvs { get; set; }

        /// <summary>
        /// The list of normals associated with this geometry.
        /// </summary>
        [IgnoreDataMember]
        internal List<float> RawNormals { get; set; }

        [DataMember(Name = "normals")]
        public List<float> Normals { get => RawNormals; set => RawNormals = value; }

        internal GeometryData()
        {
            RawVertices = [];
            Colors = [];
            Faces = [];
            RawNormals = [];
            Uvs = [];
        }

    }

    /// <summary>
    /// Class for storing geometry face data.
    /// </summary>
    [DataContract]
    public class GeometryFace
    {
        /// <summary>
        /// False for triangle, true for quad.
        /// </summary>
        [DataMember(Name = "topology")]
        public bool Topology { get; set; } //false for triangle, true for quad

        [DataMember(Name = "faceMaterial")]
        public bool FaceMaterial { get; set; }

        [DataMember(Name = "faceUvs")]
        public bool FaceUVs { get; set; }

        [DataMember(Name = "faceVertexUvs")]
        public bool FaceVertexUVs { get; set; }

        [DataMember(Name = "faceNormals")]
        public bool FaceNormals { get; set; }

        [DataMember(Name = "vertexNormals")]
        public bool VertexNormals { get; set; }

        [DataMember(Name = "faceColor")]
        public bool FaceColor { get; set; }

        [DataMember(Name = "vertexColors")]
        public bool VertexColors { get; set; }

        internal byte GetFaceType()
        {
            bool[] faceBits = new bool[] { Topology, FaceMaterial, FaceUVs, FaceVertexUVs,
                                           FaceNormals, VertexNormals, FaceColor, VertexColors };
            System.Collections.BitArray bits = new(faceBits);

            byte b = 0;
            if (bits.Get(0)) b++;
            if (bits.Get(1)) b += 2;
            if (bits.Get(2)) b += 4;
            if (bits.Get(3)) b += 8;
            if (bits.Get(4)) b += 16;
            if (bits.Get(5)) b += 32;
            if (bits.Get(6)) b += 64;
            if (bits.Get(7)) b += 128;
            return b;
        }
    }

    // public class GeometrySerializationAdapter : SerializationAdapter
    // {
    // 	/// <summary>
    // 	/// Geometry data.
    // 	/// </summary>
    // 	[DataMember(Order = 1)]
    // 	public GeometryData Data { get; set; }
    // 	public GeometrySerializationAdapter()
    // 	{
    // 		Metadata = new Metadata
    // 		{
    // 			Type = "Geometry",
    // 			Version = 4.5, // 3
    // 			Generator = "ThreeLib-Geometry.toJSON"
    // 		};
    // 		Data = new GeometryData();
    // 	}
    // }
}
