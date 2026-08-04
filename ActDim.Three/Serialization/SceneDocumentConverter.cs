using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.Objects;
using ActDim.Three.Textures;

namespace ActDim.Three.Serialization
{
    /// <summary>
    /// Newtonsoft converter for the three.js "Object" document. Structure (pools, uuid references, dedup)
    /// comes from <see cref="DocumentGraph"/>; resource/node bodies are delegated to a private serializer
    /// with the camelCase resolver and the typed-buffer / element-discriminator converters.
    /// </summary>
    public class SceneDocumentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SceneDocument);

        // Format settings live here — consumers just call plain JsonConvert on SceneDocument; its
        // [JsonConverter] routes here, so no settings/wrapper is needed.
        private static readonly JsonSerializer Inner = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new CamelCaseCustomResolver(),
            Converters = { new BufferAttributeConverter(), new ElementConverter() },
        });

        #region Write

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var document = (SceneDocument)value;
            var pools = DocumentGraph.Flatten(document.Object);

            writer.WriteStartObject();

            writer.WritePropertyName("metadata");
            Inner.Serialize(writer, document.Metadata ?? new Metadata { Version = 4.5, Type = "Object", Generator = "ThreeLib-Object3D.toJSON" });

            WritePool(writer, "geometries", pools.Geometries);
            WritePool(writer, "materials", pools.Materials);
            if (pools.Textures.Count > 0)
            {
                WritePool(writer, "textures", pools.Textures);
            }
            if (pools.Images.Count > 0)
            {
                WritePool(writer, "images", pools.Images);
            }
            if (pools.Fonts.Count > 0)
            {
                WritePool(writer, "fonts", pools.Fonts);
            }

            if (document.Object != null)
            {
                writer.WritePropertyName("object");
                Inner.Serialize(writer, document.Object);
            }

            writer.WriteEndObject();
        }

        private static void WritePool(JsonWriter writer, string name, List<IElement> pool)
        {
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var element in pool)
            {
                Inner.Serialize(writer, element);
            }
            writer.WriteEndArray();
        }

        #endregion

        #region Read

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jo = JObject.Load(reader);

            var geometryPairs = ReadPool<IElement>(jo, "geometries");
            var materialPairs = ReadPool<IElement>(jo, "materials");
            var texturePairs = ReadPool<Texture>(jo, "textures");
            var imagePairs = ReadPool<Image>(jo, "images");
            var fontPairs = ReadPool<FontData>(jo, "fonts");

            var document = new SceneDocument
            {
                Metadata = jo["metadata"]?.ToObject<Metadata>(Inner),
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

            if (jo["object"] is JObject root)
            {
                document.Object = ReadNode(root, geometriesById, materialsById);
            }

            return document;
        }

        private static List<(JObject Json, IElement Element)> ReadPool<T>(JObject jo, string name) where T : IElement
        {
            var result = new List<(JObject, IElement)>();
            if (jo[name] is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject element)
                    {
                        result.Add((element, element.ToObject<T>(Inner)));
                    }
                }
            }
            return result;
        }

        private static List<IElement> Elements(List<(JObject Json, IElement Element)> pairs)
        {
            return pairs.ConvertAll(pair => pair.Element);
        }

        private static void ResolveResourceReferences(
            List<(JObject Json, IElement Element)> materials,
            List<(JObject Json, IElement Element)> textures,
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

        private static bool TryUuid(JObject json, string key, out Guid id)
        {
            id = Guid.Empty;
            return json[key] is JValue value
                && value.Type == JTokenType.String
                && Guid.TryParse((string)value, out id);
        }

        private static Object3D ReadNode(JObject node, Dictionary<Guid, IElement> geometries, Dictionary<Guid, IElement> materials)
        {
            var obj = DocumentGraph.CreateNode((string)node["type"]);

            // Type-specific scalars (light color/intensity, scene background, visible, userData, …) come
            // from reflection; structural parts (matrix, refs, children) are wired by hand below.
            var children = node["children"];
            node.Remove("children");
            using (var subReader = node.CreateReader())
            {
                Inner.Populate(subReader, obj);
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
                        obj.Add(ReadNode(childObject, geometries, materials));
                    }
                }
            }

            return obj;
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

            var materialToken = node["material"];
            if (materialToken is JValue single && single.Type == JTokenType.String)
            {
                DocumentGraph.SetMaterial(obj, ResolveMaterial((string)single, materials));
            }
            else if (materialToken is JArray array && obj is Mesh mesh)
            {
                foreach (var item in array)
                {
                    if (item.Type == JTokenType.String)
                    {
                        mesh.Materials.Add(ResolveMaterial((string)item, materials));
                    }
                }
            }
        }

        private static IMaterial ResolveMaterial(string uuid, Dictionary<Guid, IElement> materials)
        {
            var id = Guid.Parse(uuid);
            if (!materials.TryGetValue(id, out var material))
            {
                throw new JsonSerializationException($"Unresolved material reference '{id}'.");
            }
            return (IMaterial)material;
        }

        #endregion
    }
}
