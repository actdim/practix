using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    /// <summary>
    /// Parametric plane geometry descriptor.
    /// Analogous to https://threejs.org/docs/#api/en/geometries/PlaneGeometry
    /// </summary>
    [DataContract]
    public class PlaneGeometry : BufferGeometry
    {
        public float Width { get; set; } = 1;
        public float Height { get; set; } = 1;
        public int WidthSegments { get; set; } = 1;
        public int HeightSegments { get; set; } = 1;

        public PlaneGeometry() : base()
        {
        }

        public PlaneGeometry(float width = 1, float height = 1, int widthSegments = 1, int heightSegments = 1) : this()
        {
            Width = width;
            Height = height;
            WidthSegments = widthSegments;
            HeightSegments = heightSegments;
        }
    }
}
