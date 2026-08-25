using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    /// <summary>
    /// Parametric cylinder geometry descriptor.
    /// Analogous to https://threejs.org/docs/#api/en/geometries/CylinderGeometry
    /// </summary>
    [DataContract]
    public class CylinderGeometry : BufferGeometry
    {
        public float RadiusTop { get; set; } = 1;
        public float RadiusBottom { get; set; } = 1;
        public float Height { get; set; } = 1;
        public int RadialSegments { get; set; } = 32;
        public int HeightSegments { get; set; } = 1;
        public bool OpenEnded { get; set; } = false;

        public CylinderGeometry() : base()
        {
        }

        public CylinderGeometry(float radiusTop = 1, float radiusBottom = 1, float height = 1, int radialSegments = 32, int heightSegments = 1, bool openEnded = false) : this()
        {
            RadiusTop = radiusTop;
            RadiusBottom = radiusBottom;
            Height = height;
            RadialSegments = radialSegments;
            HeightSegments = heightSegments;
            OpenEnded = openEnded;
        }
    }
}
