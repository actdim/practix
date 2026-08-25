using System.Collections.Generic;
using ActDim.Three.Core;

namespace ActDim.Three
{
    /// <summary>
    /// The three.js "Object" document (JSON Object/Scene format 4): metadata + flat resource pools
    /// (referenced by uuid) + the node tree. This is the format-facing type; the core domain objects it
    /// wraps stay attribute-free and can be serialized on their own. All three.js rules (pools, uuid
    /// references, dedup, field names) live in <see cref="Serialization.SceneDocumentStjConverter"/> (System.Text.Json).
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(Serialization.SceneDocumentStjConverter))]
    public class SceneDocument
    {
        public Metadata Metadata { get; set; }

        /// <summary>The root node of the graph (a <see cref="Scene"/> or any <see cref="Object3D"/>).</summary>
        public Object3D Object { get; set; }

        // Flat pools. Populated on read; on write they are computed from the graph by the converter.
        public List<IElement> Geometries { get; set; } = [];
        public List<IElement> Materials { get; set; } = [];
        public List<IElement> Textures { get; set; } = [];
        public List<IElement> Images { get; set; } = [];
        public List<IElement> Fonts { get; set; } = [];

        /// <summary>Builds a document from one or more root objects (no <see cref="Scene"/> required).</summary>
        public static SceneDocument From(params Object3D[] objects)
        {
            return ((IEnumerable<Object3D>)objects).ToSceneDocument();
        }
    }
}
