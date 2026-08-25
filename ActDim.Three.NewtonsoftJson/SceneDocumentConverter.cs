using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ActDim.Three.Cameras;
using ActDim.Three.Core;
using ActDim.Three.Lights;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.Objects;
using ActDim.Three.Serialization;
using ActDim.Three.Textures;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// Newtonsoft converter for the three.js "Object" document format (<see cref="SceneDocument"/>).
    /// </summary>
    public class SceneDocumentConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => objectType == typeof(SceneDocument);

        private static readonly JsonSerializer Inner = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new CamelCaseCustomResolver(),
            Converters = { new BufferAttributeConverter(), new ElementConverter() },
        });

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var doc = (SceneDocument)value;

            // Structure (pools, uuid references, dedup) comes from DocumentGraph
            var pools = DocumentGraph.Flatten(doc.Object);

            writer.WriteStartObject();

            writer.WritePropertyName("metadata");
            Inner.Serialize(writer, doc.Metadata ?? new Metadata { Version = 4.5, Type = "Object", Generator = "ActDim.Three" });

            if (pools.Geometries.Count > 0)
            {
                writer.WritePropertyName("geometries");
                Inner.Serialize(writer, pools.Geometries);
            }

            if (pools.Materials.Count > 0)
            {
                writer.WritePropertyName("materials");
                Inner.Serialize(writer, pools.Materials);
            }

            if (pools.Textures.Count > 0)
            {
                writer.WritePropertyName("textures");
                Inner.Serialize(writer, pools.Textures);
            }

            if (pools.Images.Count > 0)
            {
                writer.WritePropertyName("images");
                Inner.Serialize(writer, pools.Images);
            }

            if (pools.Fonts.Count > 0)
            {
                writer.WritePropertyName("fonts");
                Inner.Serialize(writer, pools.Fonts);
            }

            writer.WritePropertyName("object");
            Inner.Serialize(writer, doc.Object);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var rootObj = JObject.Load(reader);

            var doc = new SceneDocument();

            if (rootObj["metadata"] is JObject metadataObj)
            {
                doc.Metadata = metadataObj.ToObject<Metadata>(Inner);
            }

            var geometries = ReadPool<BufferGeometry>(rootObj["geometries"]);
            var materials = ReadPool<Material>(rootObj["materials"]);
            var textures = ReadPool<Texture>(rootObj["textures"]);
            var images = ReadPool<Image>(rootObj["images"]);

            var geometriesMap = ToMap(geometries);
            var materialsMap = ToMap(materials);
            var texturesMap = ToMap(textures);
            var imagesMap = ToMap(images);

            // Wire textures -> images and materials -> textures
            ResolveResourceReferences(materials, textures, texturesMap, imagesMap);

            doc.Geometries.AddRange(ToElements(geometries));
            doc.Materials.AddRange(ToElements(materials));
            doc.Textures.AddRange(ToElements(textures));
            doc.Images.AddRange(ToElements(images));

            if (rootObj["object"] is JObject nodeObj)
            {
                doc.Object = ReadNodeGraph(nodeObj, geometriesMap, materialsMap);
            }

            return doc;
        }

        private static List<(JObject Json, IElement Element)> ReadPool<T>(JToken poolToken) where T : class, IElement
        {
            var result = new List<(JObject Json, IElement Element)>();
            if (poolToken is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject obj)
                    {
                        var typeToken = obj["type"] ?? obj["Type"];
                        Type targetType = null;
                        if (typeToken != null)
                        {
                            targetType = DocumentGraph.ElementType(typeToken.Value<string>());
                        }
                        targetType ??= typeof(T);

                        var elem = (IElement)obj.ToObject(targetType, Inner);
                        if (elem != null)
                        {
                            result.Add((obj, elem));
                        }
                    }
                }
            }
            return result;
        }

        private static Dictionary<Guid, IElement> ToMap(List<(JObject Json, IElement Element)> pool)
        {
            var map = new Dictionary<Guid, IElement>();
            foreach (var (_, element) in pool)
            {
                map[element.Uuid] = element;
            }
            return map;
        }

        private static IEnumerable<IElement> ToElements(List<(JObject Json, IElement Element)> pool)
        {
            foreach (var (_, element) in pool)
            {
                yield return element;
            }
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
            if (json[key] is JValue val && val.Value != null && Guid.TryParse(val.Value.ToString(), out id))
            {
                return true;
            }
            return false;
        }

        private static Object3D ReadNodeGraph(
            JObject nodeObj,
            Dictionary<Guid, IElement> geometriesMap,
            Dictionary<Guid, IElement> materialsMap)
        {
            var typeStr = nodeObj["type"]?.Value<string>();
            Object3D node = DocumentGraph.CreateNode(typeStr);
            Inner.Populate(nodeObj.CreateReader(), node);

            if (node == null)
            {
                return null;
            }

            // Restore node matrix
            if (nodeObj["matrix"] is JArray matrixArr && matrixArr.Count == 16)
            {
                var elements = new float[16];
                for (var i = 0; i < 16; i++)
                {
                    elements[i] = (float)matrixArr[i].Value<double>();
                }
                node.Matrix = new Matrix4 { Elements = elements };
            }

            // Resolve geometry reference
            if (nodeObj["geometry"] is JValue geomUuidToken && geomUuidToken.Value != null)
            {
                if (Guid.TryParse(geomUuidToken.Value.ToString(), out var geomGuid) &&
                    geometriesMap.TryGetValue(geomGuid, out var geom) && geom is IGeometry typedGeom)
                {
                    if (node is Mesh meshNode)
                    {
                        meshNode.Geometry = typedGeom;
                    }
                    else if (node is Line lineNode)
                    {
                        lineNode.Geometry = typedGeom;
                    }
                    else if (node is Points pointsNode)
                    {
                        pointsNode.Geometry = typedGeom;
                    }
                }
            }

            // Resolve material reference (single UUID or array of UUIDs for multi-material mesh)
            if (nodeObj["material"] is JArray matArray && node is Mesh multiMesh)
            {
                multiMesh.Materials.Clear();
                foreach (var item in matArray)
                {
                    if (item is JValue val && val.Value != null &&
                        Guid.TryParse(val.Value.ToString(), out var matGuid) &&
                        materialsMap.TryGetValue(matGuid, out var mat) && mat is IMaterial typedMat)
                    {
                        multiMesh.Materials.Add(typedMat);
                    }
                }
            }
            else if (nodeObj["material"] is JValue matUuidToken && matUuidToken.Value != null)
            {
                if (Guid.TryParse(matUuidToken.Value.ToString(), out var matGuid) &&
                    materialsMap.TryGetValue(matGuid, out var mat) && mat is IMaterial typedMat)
                {
                    DocumentGraph.SetMaterial(node, typedMat);
                }
            }

            // Recursively deserialize children
            if (nodeObj["children"] is JArray childrenArray)
            {
                node.Children.Clear();
                foreach (var childToken in childrenArray)
                {
                    if (childToken is JObject childObj)
                    {
                        var childNode = ReadNodeGraph(childObj, geometriesMap, materialsMap);
                        if (childNode != null)
                        {
                            node.Add(childNode);
                        }
                    }
                }
            }

            return node;
        }
    }
}
