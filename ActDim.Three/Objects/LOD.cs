using System.Collections.Generic;
using System.Runtime.Serialization;
using ActDim.Three.Core;

namespace ActDim.Three.Objects
{
    [DataContract]
    public class LODLevel
    {
        [DataMember(Name = "distance")]
        public float Distance { get; set; }

        [DataMember(Name = "hysteresis")]
        public float Hysteresis { get; set; }

        [DataMember(Name = "object")]
        public Object3D Object { get; set; }
    }

    /// <summary>
    /// Level of Detail (LOD) node manager.
    /// Analogous to https://threejs.org/docs/#api/en/objects/LOD
    /// </summary>
    [DataContract]
    public class LOD : Object3D
    {
        [DataMember(Name = "levels")]
        public List<LODLevel> Levels { get; set; } = new List<LODLevel>();

        public LOD() : base()
        {
        }

        public void AddLevel(Object3D object3d, float distance = 0, float hysteresis = 0)
        {
            Levels.Add(new LODLevel { Object = object3d, Distance = distance, Hysteresis = hysteresis });
            Add(object3d);
        }
    }
}
