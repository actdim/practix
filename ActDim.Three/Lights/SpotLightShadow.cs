using ActDim.Three.Cameras;

namespace ActDim.Three.Lights
{
    /// <summary>
    /// 
    /// Analogous to: https://github.com/mrdoob/three.js/blob/master/src/lights/SpotLightShadow.js
    /// Original Source: https://github.com/mrdoob/three.js/blob/master/src/lights/SpotLightShadow.js
    /// </summary>
    public class SpotLightShadow : LightShadow
    {
        public new PerspectiveCamera Camera { get; set; }
    }
}