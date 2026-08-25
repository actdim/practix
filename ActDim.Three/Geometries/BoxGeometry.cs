using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Geometries
{
    /// <summary>
    /// Parametric box geometry descriptor.
    /// Analogous to https://threejs.org/docs/#api/en/geometries/BoxGeometry
    /// </summary>
    [DataContract]
    public class BoxGeometry : BufferGeometry
    {
        public float Width { get; set; } = 1;
        public float Height { get; set; } = 1;
        public float Depth { get; set; } = 1;
        public int WidthSegments { get; set; } = 1;
        public int HeightSegments { get; set; } = 1;
        public int DepthSegments { get; set; } = 1;

        public BoxGeometry() : base()
        {
        }

        public BoxGeometry(float width, float height, float depth, int widthSegments = 1, int heightSegments = 1, int depthSegments = 1) : this()
        {
            Width = width;
            Height = height;
            Depth = depth;
            WidthSegments = widthSegments;
            HeightSegments = heightSegments;
            DepthSegments = depthSegments;
        }
    }
}
