using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.Objects;
using ActDim.Three.Textures;

namespace ActDim.Three.Serialization
{
    /// <summary>
    /// System.Text.Json counterpart of <see cref="SceneDocumentConverter"/>. Structure comes from
    /// <see cref="DocumentGraph"/>; resource/node bodies go through a private <see cref="JsonSerializerOptions"/>
    /// with <see cref="DataContractResolver"/> (three.js names) and <see cref="BufferAttributeStjConverter"/>
    /// (typed buffers). Nodes are written with their runtime type and their children re-written
    /// polymorphically, since STJ otherwise serializes a <c>List&lt;Object3D&gt;</c> by its base type.
    /// </summary>
    public sealed class SceneDocumentStjConverter : JsonConverter<SceneDocument>
    {
        private static readonly JsonSerializerOptions Inner = CreateInner();

        private static JsonSerializerOptions CreateInner()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = DataContractResolver.Instance,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            };
            options.Converters.Add(new BufferAttributeStjConverter());
            return options;
        }

        #region Write

        public override void Write(Utf8JsonWriter writer, SceneDocument document, JsonSerializerOptions options)
        {
            var pools = DocumentGraph.Flatten(document.Object);

            var root = new JsonObject
            {
                ["metadata"] = JsonSerializer.SerializeToNode(
                    document.Metadata ?? new Metadata { Version = 4.5, Type = "Object", Generator = "ThreeLib-Object3D.toJSON" },
                    Inner),
                ["geometries"] = PoolToNode(pools.Geometries),
                ["materials"] = PoolToNode(pools.Materials),
            };

            if (pools.Textures.Count > 0)
            {
                root["textures"] = PoolToNode(pools.Textures);
            }
            if (pools.Images.Count > 0)
            {
                root["images"] = PoolToNode(pools.Images);
            }
            if (pools.Fonts.Count > 0)
            {
                root["fonts"] = PoolToNode(pools.Fonts);
            }

            if (document.Object != null)
            {
                root["object"] = WriteNode(document.Object);
            }

            root.WriteTo(writer);
        }

        private static JsonArray PoolToNode(List<IElement> pool)
        {
            var array = new JsonArray();
            foreach (var element in pool)
            {
                array.Add(JsonSerializer.SerializeToNode(element, element.GetType(), Inner));
            }
            return array;
        }

        private static JsonObject WriteNode(Object3D node)
        {
            var obj = JsonSerializer.SerializeToNode(node, node.GetType(), Inner).AsObject();

            // STJ serialized `children` by the base Object3D type; rewrite it polymorphically.
            var children = new JsonArray();
            foreach (var child in node.Children)
            {
                children.Add(WriteNode(child));
            }
            obj["children"] = children;

            return obj;
        }

        #endregion

        #region Read

        public override SceneDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonNode.Parse(ref reader) is not JsonObject root)
            {
                return null;
            }

            var geometryPairs = ReadPoolPolymorphic(root, "geometries");
            var materialPairs = ReadPoolPolymorphic(root, "materials");
            var texturePairs = ReadPoolConcrete<Texture>(root, "textures");
            var imagePairs = ReadPoolConcrete<Image>(root, "images");
            var fontPairs = ReadPoolConcrete<FontData>(root, "fonts");

            var document = new SceneDocument
            {
                Metadata = root["metadata"]?.Deserialize<Metadata>(Inner),
                Geometries = Elements(geometryPairs),
                Materials = Elements(materialPairs),
                Textures = Elements(texturePairs),
                Images = Elements(imagePairs),
                Fonts = Elements(fontPairs),
            };

            var geometriesById = DocumentGraph.ToUuidMap(document.Geometries);
            var materialsById = DocumentGraph.ToUuidMap(document.Materials);
            var texturesById = DocumentGraph.ToUuidMap(document.Textures);
            var imagesById = DocumentGraph.ToUuidMap(document.Images);

            ResolveResourceReferences(materialPairs, texturePairs, texturesById, imagesById);

            if (root["object"] is JsonObject node)
            {
                document.Object = ReadNode(node, geometriesById, materialsById);
            }

            return document;
        }

        private static List<(JsonObject Json, IElement Element)> ReadPoolPolymorphic(JsonObject root, string name)
        {
            var result = new List<(JsonObject, IElement)>();
            if (root[name] is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is JsonObject jo)
                    {
                        var type = (string)jo["type"];
                        var concrete = DocumentGraph.ElementType(type)
                            ?? throw new JsonException($"Unknown element type '{type}'.");
                        result.Add((jo, (IElement)jo.Deserialize(concrete, Inner)));
                    }
                }
            }
            return result;
        }

        private static List<(JsonObject Json, IElement Element)> ReadPoolConcrete<T>(JsonObject root, string name) where T : IElement
        {
            var result = new List<(JsonObject, IElement)>();
            if (root[name] is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is JsonObject jo)
                    {
                        result.Add((jo, jo.Deserialize<T>(Inner)));
                    }
                }
            }
            return result;
        }

        private static List<IElement> Elements(List<(JsonObject Json, IElement Element)> pairs)
        {
            return pairs.ConvertAll(pair => pair.Element);
        }

        private static void ResolveResourceReferences(
            List<(JsonObject Json, IElement Element)> materials,
            List<(JsonObject Json, IElement Element)> textures,
            Dictionary<Guid, IElement> texturesById,
            Dictionary<Guid, IElement> imagesById)
        {
            foreach (var (json, element) in textures)
            {
                if (element is Texture texture && TryUuid(json, "image", out var id) && imagesById.TryGetValue(id, out var image))
                {
                    texture.Image = (Image)image;
                }
            }

            foreach (var (json, element) in materials)
            {
                foreach (var (key, textureProperty) in DocumentGraph.TextureSlots(element.GetType()))
                {
                    if (TryUuid(json, key, out var id) && texturesById.TryGetValue(id, out var texture))
                    {
                        textureProperty.SetValue(element, texture);
                    }
                }
            }
        }

        private static bool TryUuid(JsonObject json, string key, out Guid id)
        {
            id = Guid.Empty;
            return json[key] is JsonValue value
                && value.TryGetValue<string>(out var text)
                && Guid.TryParse(text, out id);
        }

        private static Object3D ReadNode(JsonObject node, Dictionary<Guid, IElement> geometries, Dictionary<Guid, IElement> materials)
        {
            var type = (string)node["type"];

            JsonNode childrenNode = null;
            if (node.ContainsKey("children"))
            {
                childrenNode = node["children"];
                node.Remove("children");
            }

            // Scalars (light color/intensity, scene background, visible, userData, …) via reflection;
            // structure (matrix, refs, children) wired by hand below.
            var obj = (Object3D)node.Deserialize(DocumentGraph.NodeType(type), Inner);
            obj.Children.Clear();

            if (node["matrix"] is JsonArray matrix && matrix.Count == 16)
            {
                var elements = new float[16];
                for (var i = 0; i < 16; i++)
                {
                    elements[i] = (float)matrix[i].GetValue<double>();
                }
                obj.Matrix = new Matrix4 { Elements = elements };
            }

            ResolveReferences(obj, node, geometries, materials);

            if (childrenNode is JsonArray children)
            {
                foreach (var child in children)
                {
                    if (child is JsonObject childObject)
                    {
                        obj.Add(ReadNode(childObject, geometries, materials));
                    }
                }
            }

            return obj;
        }

        private static void ResolveReferences(Object3D obj, JsonObject node, Dictionary<Guid, IElement> geometries, Dictionary<Guid, IElement> materials)
        {
            if (obj is IGeometryContainer container && node["geometry"] is JsonValue g && g.TryGetValue<string>(out var geometryUuid))
            {
                var id = Guid.Parse(geometryUuid);
                if (!geometries.TryGetValue(id, out var geometry))
                {
                    throw new JsonException($"Unresolved geometry reference '{id}'.");
                }
                container.Geometry = (IGeometry)geometry;
            }

            var materialNode = node["material"];
            if (materialNode is JsonValue single && single.TryGetValue<string>(out var materialUuid))
            {
                DocumentGraph.SetMaterial(obj, ResolveMaterial(materialUuid, materials));
            }
            else if (materialNode is JsonArray array && obj is Mesh mesh)
            {
                foreach (var item in array)
                {
                    if (item is JsonValue value && value.TryGetValue<string>(out var uuid))
                    {
                        mesh.Materials.Add(ResolveMaterial(uuid, materials));
                    }
                }
            }
        }

        private static IMaterial ResolveMaterial(string uuid, Dictionary<Guid, IElement> materials)
        {
            var id = Guid.Parse(uuid);
            if (!materials.TryGetValue(id, out var material))
            {
                throw new JsonException($"Unresolved material reference '{id}'.");
            }
            return (IMaterial)material;
        }

        #endregion
    }
}
