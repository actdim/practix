using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    /// <summary>
    /// Parametric sphere geometry descriptor.
    /// Analogous to https://threejs.org/docs/#api/en/geometries/SphereGeometry
    /// </summary>
    [DataContract]
    public class SphereGeometry : BufferGeometry
    {
        public float Radius { get; set; } = 1;
        public int WidthSegments { get; set; } = 32;
        public int HeightSegments { get; set; } = 16;
        public float PhiStart { get; set; } = 0;
        public float PhiLength { get; set; } = 6.283185307179586f;
        public float ThetaStart { get; set; } = 0;
        public float ThetaLength { get; set; } = 3.141592653589793f;

        public SphereGeometry() : base()
        {
        }

        public SphereGeometry(float radius = 1, int widthSegments = 32, int heightSegments = 16) : this()
        {
            Radius = radius;
            WidthSegments = widthSegments;
            HeightSegments = heightSegments;
        }
    }
}
