using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using THREE.Core;
using THREE.Materials;
using THREE.Math;
using THREE.Objects;
using THREE.Textures;

namespace THREE.Serialization
{
    /// <summary>
    /// Owns all three.js document rules. On write it walks the (attribute-free) core graph, builds the
    /// flat resource pools with identity dedup, assigns missing uuids, and emits
    /// <c>{ metadata, geometries, materials, textures?, images?, fonts?, object }</c>. Resource and node
    /// bodies are delegated to the serializer (reflection + the camelCase resolver) — the converter only
    /// controls document structure.
    /// <para>
    /// Read is currently shallow (plan §8): pools are resolved polymorphically via
    /// <see cref="ElementConverter"/>, the node tree is materialized as base <see cref="Object3D"/> nodes
    /// (type string preserved). Full node polymorphism + uuid reference resolution + matrix read are a
    /// later milestone.
    /// </para>
    /// </summary>
    public class SceneDocumentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SceneDocument);

        #region Write (свёртка)

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var document = (SceneDocument)value;
            var pools = new PoolContext();

            if (document.Object != null)
            {
                Collect(document.Object, pools);
            }

            writer.WriteStartObject();

            writer.WritePropertyName("metadata");
            serializer.Serialize(writer, document.Metadata ?? new Metadata { Version = 4.5, Type = "Object", Generator = "ThreeLib-Object3D.toJSON" });

            WritePool(writer, serializer, "geometries", pools.Geometries);
            WritePool(writer, serializer, "materials", pools.Materials);
            if (pools.Textures.Count > 0)
            {
                WritePool(writer, serializer, "textures", pools.Textures);
            }
            if (pools.Images.Count > 0)
            {
                WritePool(writer, serializer, "images", pools.Images);
            }
            if (pools.Fonts.Count > 0)
            {
                WritePool(writer, serializer, "fonts", pools.Fonts);
            }

            if (document.Object != null)
            {
                writer.WritePropertyName("object");
                serializer.Serialize(writer, document.Object);
            }

            writer.WriteEndObject();
        }

        private static void WritePool(JsonWriter writer, JsonSerializer serializer, string name, List<IElement> pool)
        {
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var element in pool)
            {
                serializer.Serialize(writer, element);
            }
            writer.WriteEndArray();
        }

        private static void Collect(Object3D node, PoolContext pools)
        {
            EnsureUuid(node);

            if (node is IGeometryContainer container && container.Geometry != null)
            {
                EnsureUuid(container.Geometry);
                pools.AddUnique(pools.Geometries, container.Geometry);
            }

            var material = MaterialOf(node);
            if (material != null)
            {
                EnsureUuid(material);
                if (pools.AddUnique(pools.Materials, material))
                {
                    CollectTextures(material, pools);
                }
            }

            foreach (var child in node.Children)
            {
                Collect(child, pools);
            }
        }

        private static void CollectTextures(IMaterial material, PoolContext pools)
        {
            if (!(material is MeshStandardMaterial standard))
            {
                return;
            }

            foreach (var kvp in standard.GetTextures())
            {
                var texture = kvp.Value;
                if (texture == null)
                {
                    continue;
                }

                EnsureUuid(texture);
                if (pools.AddUnique(pools.Textures, texture) && texture.Image != null)
                {
                    EnsureUuid(texture.Image);
                    pools.AddUnique(pools.Images, texture.Image);
                }
            }
        }

        private static IMaterial MaterialOf(Object3D node)
        {
            switch (node)
            {
                case Mesh mesh: return mesh.Material;
                case Line line: return line.Material;
                case LineSegments segments: return segments.Material;
                case Points points: return points.Material;
                default: return null;
            }
        }

        private static void EnsureUuid(IElement element)
        {
            if (element.Uuid == Guid.Empty)
            {
                element.Uuid = Guid.NewGuid();
            }
        }

        private sealed class PoolContext
        {
            public readonly List<IElement> Geometries = new List<IElement>();
            public readonly List<IElement> Materials = new List<IElement>();
            public readonly List<IElement> Textures = new List<IElement>();
            public readonly List<IElement> Images = new List<IElement>();
            public readonly List<IElement> Fonts = new List<IElement>();

            private readonly HashSet<object> _seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

            /// <summary>Adds by reference identity; returns true if newly added.</summary>
            public bool AddUnique(List<IElement> pool, IElement element)
            {
                if (!_seen.Add(element))
                {
                    return false;
                }
                pool.Add(element);
                return true;
            }
        }

        #endregion

        #region Read (развёртка)

        private static readonly Dictionary<string, Type> NodeTypes = BuildNodeTypes();

        private static Dictionary<string, Type> BuildNodeTypes()
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in typeof(Object3D).Assembly.GetTypes())
            {
                if (t.IsAbstract || !typeof(Object3D).IsAssignableFrom(t))
                {
                    continue;
                }
                if (t.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }
                map[t.Name] = t;
            }
            return map;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jo = JObject.Load(reader);

            var document = new SceneDocument
            {
                Metadata = jo["metadata"]?.ToObject<Metadata>(serializer),
                Geometries = ReadPool(jo, "geometries", serializer),
                Materials = ReadPool(jo, "materials", serializer),
                Textures = ReadPool(jo, "textures", serializer),
                Images = ReadPool(jo, "images", serializer),
                Fonts = ReadPool(jo, "fonts", serializer),
            };

            var geometries = ToUuidMap(document.Geometries);
            var materials = ToUuidMap(document.Materials);

            if (jo["object"] is JObject root)
            {
                document.Object = ReadNode(root, geometries, materials, serializer);
            }

            return document;
        }

        private static List<IElement> ReadPool(JObject jo, string name, JsonSerializer serializer)
        {
            var token = jo[name];
            if (token == null || token.Type != JTokenType.Array)
            {
                return new List<IElement>();
            }
            return token.ToObject<List<IElement>>(serializer);
        }

        private static Dictionary<Guid, IElement> ToUuidMap(List<IElement> pool)
        {
            var map = new Dictionary<Guid, IElement>();
            foreach (var element in pool)
            {
                map[element.Uuid] = element;
            }
            return map;
        }

        private static Object3D ReadNode(JObject node, Dictionary<Guid, IElement> geometries, Dictionary<Guid, IElement> materials, JsonSerializer serializer)
        {
            var type = (string)node["type"];
            var obj = CreateNode(type);

            // Type-specific scalars (light color/intensity, scene background, visible, userData, …) come
            // from reflection; structural parts (matrix, refs, children) are wired by hand below.
            var children = node["children"];
            node.Remove("children");
            using (var subReader = node.CreateReader())
            {
                serializer.Populate(subReader, obj);
            }

            if (node["matrix"] is JArray matrix && matrix.Count == 16)
            {
                var elements = new float[16];
                for (var i = 0; i < 16; i++)
                {
                    elements[i] = (float)matrix[i];
                }
                obj.Matrix = new Matrix4 { Elements = elements };
            }

            ResolveReferences(obj, node, geometries, materials);

            if (children is JArray childArray)
            {
                foreach (var child in childArray)
                {
                    if (child is JObject childObject)
                    {
                        obj.Add(ReadNode(childObject, geometries, materials, serializer));
                    }
                }
            }

            return obj;
        }

        private static Object3D CreateNode(string type)
        {
            if (type != null && NodeTypes.TryGetValue(type, out var concrete))
            {
                return (Object3D)Activator.CreateInstance(concrete);
            }
            // Lenient for unmodeled node types (subset policy): keep structure as a base Object3D.
            return new Object3D { Type = type };
        }

        private static void ResolveReferences(Object3D obj, JObject node, Dictionary<Guid, IElement> geometries, Dictionary<Guid, IElement> materials)
        {
            if (obj is IGeometryContainer container && node["geometry"] is JValue g && g.Type == JTokenType.String)
            {
                var id = Guid.Parse((string)g);
                if (!geometries.TryGetValue(id, out var geometry))
                {
                    throw new JsonSerializationException($"Unresolved geometry reference '{id}'.");
                }
                container.Geometry = (IGeometry)geometry;
            }

            if (node["material"] is JValue m && m.Type == JTokenType.String)
            {
                var id = Guid.Parse((string)m);
                if (!materials.TryGetValue(id, out var material))
                {
                    throw new JsonSerializationException($"Unresolved material reference '{id}'.");
                }
                SetMaterial(obj, (IMaterial)material);
            }
        }

        private static void SetMaterial(Object3D obj, IMaterial material)
        {
            switch (obj)
            {
                case Mesh mesh: mesh.Material = material; break;
                case Line line: line.Material = material; break;
                case LineSegments segments: segments.Material = material; break;
                case Points points: points.Material = material; break;
            }
        }

        #endregion
    }
}
