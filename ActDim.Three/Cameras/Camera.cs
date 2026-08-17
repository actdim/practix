using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Math;

namespace ActDim.Three.Cameras
{
    /// <summary>
    /// Abstract base class for cameras. This class should always be inherited when you build a new camera. 
    /// Analogous to: https://threejs.org/docs/index.html#api/cameras/Camera
    /// Original Source: https://github.com/mrdoob/three.js/blob/master/src/cameras/Camera.js
    /// </summary>
    [DataContract]
    public abstract class Camera : Object3D
    {
        [DataMember(Name = "matrixWorldInverse")]
        public Matrix4 MatrixWorldInverse { get; set; }

        [DataMember(Name = "projectionMatrix")]
        public Matrix4 ProjectionMatrix { get; set; }

		public Camera()
        {

        }
    }
}
