using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three
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
        /// <summary>
        /// Background color for the scene.
        /// </summary>
        [DataMember(Name = "background")]
        public int Background { get; set; }
    }
}
