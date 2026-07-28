using System.Collections.Generic;
using Newtonsoft.Json;
using THREE.Core;

namespace THREE
{
    /// <summary>
    /// The three.js "Object" document (JSON Object/Scene format 4): metadata + flat resource pools
    /// (referenced by uuid) + the node tree. This is the format-facing type; the core domain objects it
    /// wraps stay attribute-free and can be serialized on their own. All three.js rules (pools, uuid
    /// references, dedup, field names) live in <see cref="Serialization.SceneDocumentConverter"/>.
    /// </summary>
    [JsonConverter(typeof(Serialization.SceneDocumentConverter))]
    public class SceneDocument
    {
        public Metadata Metadata { get; set; }

        /// <summary>The root node of the graph (a <see cref="Scene"/> or any <see cref="Object3D"/>).</summary>
        public Object3D Object { get; set; }

        // Flat pools. Populated on read; on write they are computed from the graph by the converter.
        public List<IElement> Geometries { get; set; } = new List<IElement>();
        public List<IElement> Materials { get; set; } = new List<IElement>();
        public List<IElement> Textures { get; set; } = new List<IElement>();
        public List<IElement> Images { get; set; } = new List<IElement>();
        public List<IElement> Fonts { get; set; } = new List<IElement>();

        /// <summary>Builds a document from one or more root objects (no <see cref="Scene"/> required).</summary>
        public static SceneDocument From(params Object3D[] objects)
        {
            return ((IEnumerable<Object3D>)objects).ToSceneDocument();
        }
    }

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

    /// <summary>
    /// Entry point for three.js document (de)serialization. Replaces the old <c>Utilities</c> wrappers.
    /// </summary>
    public static class ThreeJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new Serialization.CamelCaseCustomResolver(),
            Converters =
            {
                new Serialization.BufferAttributeConverter(),
                new Serialization.ElementConverter(),
            },
        };

        public static string Serialize(SceneDocument document, bool indented = false)
        {
            return JsonConvert.SerializeObject(document, indented ? Formatting.Indented : Formatting.None, Settings);
        }

        public static SceneDocument Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<SceneDocument>(json, Settings);
        }
    }
}
