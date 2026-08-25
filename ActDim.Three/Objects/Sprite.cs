using System;
using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;

namespace ActDim.Three.Objects
{
    /// <summary>
    /// A sprite is a 2D plane in a 3D scene that always faces the camera.
    /// Analogous to https://threejs.org/docs/#api/en/objects/Sprite
    /// </summary>
    [DataContract]
    public class Sprite : Object3D
    {
        [IgnoreDataMember]
        public IMaterial Material { get; set; }

        [DataMember(Name = "material")]
        public Guid? MaterialUuid => Material?.Uuid;

        public Sprite() : base()
        {
        }

        public Sprite(IMaterial material) : this()
        {
            Material = material;
        }
    }
}
