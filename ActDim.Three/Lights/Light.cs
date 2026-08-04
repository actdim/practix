using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Lights
{
    /// <summary>
    /// Abstract base class for lights - all other light types inherit the properties and methods described here.
    /// Analogous to: https://threejs.org/docs/index.html#api/lights/Light
    /// Original source: https://github.com/mrdoob/three.js/blob/master/src/lights/Light.js
    /// </summary>
    [DataContract]
    public abstract class Light : Object3D
    {
        /// <summary>
        /// Light color.
        /// </summary>
        [DataMember(Name = "color")]
        public int Color { get; set; }

        /// <summary>
        /// Light intensity.
        /// </summary>
        [DataMember(Name = "intensity")]
        public float Intensity { get; set; }
    }
}