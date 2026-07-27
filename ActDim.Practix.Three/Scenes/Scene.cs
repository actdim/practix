using System.Runtime.Serialization;
using THREE.Core;
using THREE.Utility;

namespace THREE
{
    /// <summary>
    /// Scenes allow you to set up what and where is to be rendered by three.js. This is where you place objects, lights and cameras.
    /// <para>Analogous to https://threejs.org/docs/index.html#api/scenes/Scene </para>
    /// <para>JS Source: https://github.com/mrdoob/three.js/blob/master/src/scenes/Scene.js</para>
    /// </summary>
    /// <example>
    /// Create a new Scene and set the Background Color and Name.
    /// <code>
    /// var scene = new Scene
    /// {
    ///     Background = new  Color(255,0,255).ToInt(),
    ///     Name = "My Scene"
    /// };
    /// </code>
    /// </example>
    public class Scene : Object3D
    {
        #region Properties

        /// <summary>
        /// Background color for the scene.
        /// </summary>		
        public int Background { get; set; }

        [IgnoreDataMember]
        internal new SceneSerializationAdapter SerializationAdapter { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Converts this Scene to a compatible JSON format.
        /// </summary>
        /// <returns>JSON String.</returns>
        public override byte[] ToJSON() // bool format
        {
            base.SerializationAdapter = new Object3DSerializationAdapter();

            ProcessChildren();

            SerializationAdapter = new SceneSerializationAdapter();
            SerializationAdapter.Object.Name = Name;
            SerializationAdapter.Object.Background = Background;
            SerializationAdapter.Object.UserData = UserData;
            SerializationAdapter.Geometries.AddRange(base.SerializationAdapter.Geometries);
            SerializationAdapter.Images = base.SerializationAdapter.Images;
            SerializationAdapter.Textures = base.SerializationAdapter.Textures;
            SerializationAdapter.Materials = base.SerializationAdapter.Materials;
            SerializationAdapter.Fonts = base.SerializationAdapter.Fonts;
            SerializationAdapter.Object.Children = base.SerializationAdapter.Object.Children;

            return Utilities.Serialize(SerializationAdapter);
        }

        #endregion

    }
    /// <summary>
    /// Internal class to format Scene object for the Three.js Object Scene Format:
    /// https://github.com/mrdoob/three.js/wiki/JSON-Object-Scene-format-4
    /// </summary>
    [DataContract]
    public class SceneSerializationAdapter : ObjectSerializationAdapter
    {
		[DataMember(Order = 5)]
        public SceneObject Object { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SceneSerializationAdapter()
        {
            Object = new SceneObject()
            {
                Type = "Scene"
            };
            Metadata.Generator = "ThreeLib-Object3D.toJSON";
        }

		public class SceneObject : Object3D
        {
            [DataMember]
            public int Background { get; set; }
        }
    }
}