using System.Collections.Generic;
using THREE.Math;
using System.Runtime.Serialization;

namespace THREE.Core
{
    /// <summary>
    /// Base class for all objects. Analogous to https://threejs.org/docs/index.html#api/core/Object3D
    /// </summary>
    [DataContract]
    public class Object3D : Element
    {
        #region Properties

        /// <summary>
        /// Object visibility.
        /// </summary>
        [DataMember(Name = "visible")]
        public bool Visible { get; set; }

        /// <summary>
        /// Flag for determining if object casts shadow.
        /// </summary>
        [DataMember(Name = "castShadow")]
        public bool CastShadow { get; set; }

        /// <summary>
        /// Flag for determining if object receives shadow.
        /// </summary>
        [DataMember(Name = "receiveShadow")]
        public bool ReceiveShadow { get; set; }

        /// <summary>
        /// List with object's children.
        /// </summary>
        [DataMember(Name = "children")]
        public List<Object3D> Children { get; set; }

        [IgnoreDataMember]
        public Object3D Parent { get; set; }

        /// <summary>
        /// Arbitrary user data (an opaque JSON object; not interpreted by this library).
        /// </summary>
        [DataMember(Name = "userData")]
        public Dictionary<string, object> UserData { get; set; }

        /// <summary>
        /// Object matrix.
        /// </summary>
        [IgnoreDataMember]
        public Matrix4 Matrix { get; set; }

        [DataMember(Name = "matrix")]
        public IEnumerable<object> MatrixArray { get { return Matrix.ToObjectList(); } }

        /// <summary>
        /// The object's local position. Independent of <see cref="Matrix"/>: the two are not kept in
        /// sync automatically; compose a matrix explicitly via <see cref="Matrix4"/> when needed.
        /// </summary>
        [IgnoreDataMember]
        public Vector3 Position { get; set; }

        [IgnoreDataMember]
        public Euler Rotation { get; set; }

        [IgnoreDataMember]
        public Quaternion Quaternion { get; set; }

        [IgnoreDataMember]
        public Vector3 Scale { get; set; }

        public static Vector3 DefaultUp { get; set; }

        #endregion

        #region Constructors

        static Object3D()
        {
            DefaultUp = new Vector3(0, 1, 0);
        }

        /// <summary>
        /// Default constructor. Results in an empty Object3D.
        /// </summary>
        public Object3D()
        {
            Children = new List<Object3D>();
            Matrix = Matrix4.Identity();
            Position = new Vector3();
            Rotation = new Euler();
            Quaternion = new Quaternion();
            Scale = new Vector3 { X = 1, Y = 1, Z = 1 };
            Parent = null;
        }

        #endregion

        #region Methods

        public void UpdateMatrix()
        {
            Matrix.Compose(Position, Quaternion, Scale);
        }

        /// <summary>
        /// Adds an object as a child of this object.
        /// </summary>
        /// <param name="obj"></param>
        public void Add(Object3D obj)
        {
            if (obj is Object3D obj3D)
            {
                obj3D.Parent = this;
            }

            Children.Add(obj);
        }

        /// <summary>
        /// Adds a list of objects as children of this object.
        /// </summary>
        /// <param name="objects"></param>
        public void AddRange(IEnumerable<Object3D> objects)
        {
            Children.AddRange(objects);
        }

        #endregion
    }
}
