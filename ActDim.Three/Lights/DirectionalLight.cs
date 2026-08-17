using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Lights
{
    /// <summary>
    /// A light that gets emitted in a specific direction.
    /// Analogous to: https://threejs.org/docs/index.html#api/lights/DirectionalLight
    /// Original Source: https://github.com/mrdoob/three.js/blob/master/src/lights/DirectionalLight.js
    /// </summary>
    public class DirectionalLight : Light
    {
        /// <summary>
        /// The directional light shadow object.
        /// </summary>
        [DataMember(Name = "shadow")]
        public DirectionalLightShadow Shadow { get; set; }

        /// <summary>
        /// The directional light points from its position to target.position.
        /// </summary>
        [DataMember(Name = "target")]
        public Object3D Target { get; set; }

		public DirectionalLight()
        {
            Shadow = new DirectionalLightShadow();
        }
    }
}
