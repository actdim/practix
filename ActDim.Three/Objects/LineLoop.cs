using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// A continuous line that connects the last vertex to the first vertex.
    /// Analogous to https://threejs.org/docs/#api/en/objects/LineLoop
    /// </summary>
    [DataContract]
    public class LineLoop : Line
    {
        public LineLoop() : base()
        {
        }

        public LineLoop(IGeometry geometry, IMaterial material) : base(geometry, material)
        {
        }
    }
}
