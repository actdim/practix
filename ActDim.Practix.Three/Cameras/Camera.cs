using System.Runtime.Serialization;
using THREE.Core;
using THREE.Math;

namespace THREE.Cameras
{
    /// <summary>
    /// Abstract base class for cameras. This class should always be inherited when you build a new camera. 
    /// Analogous to: https://threejs.org/docs/index.html#api/cameras/Camera
    /// Original Source: https://github.com/mrdoob/three.js/blob/master/src/cameras/Camera.js
    /// </summary>
    [DataContract]
    public abstract class Camera : Object3D
    {
        [DataMember]
        public Matrix4 MatrixWorldInverse { get; set; }

        [DataMember]
        public Matrix4 ProjectionMatrix { get; set; }

		public Camera()
        {

        }
    }
}
