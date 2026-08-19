using System.Collections.Generic;
using ActDim.Three.Core;

namespace ActDim.Three.Core
{
    /// <summary>Extensions for producing a <see cref="SceneDocument"/> from domain objects.</summary>
    public static class SceneDocumentExtensions
    {
        public static SceneDocument ToSceneDocument(this Object3D root)
        {
            return new SceneDocument
            {
                Object = root,
                Metadata = new Metadata { Version = 4.5, Type = "Object", Generator = "ThreeLib-Object3D.toJSON" },
            };
        }

        /// <summary>Wraps several roots under a single container node, then builds the document.</summary>
        public static SceneDocument ToSceneDocument(this IEnumerable<Object3D> roots)
        {
            var container = new Object3D();
            foreach (var root in roots)
            {
                container.Add(root);
            }
            return container.ToSceneDocument();
        }
    }
}
