using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Objects;
using ActDim.Three.Textures;

namespace ActDim.Three.Serialization
{
    /// <summary>
    /// Serializer-agnostic three.js document logic shared by the Newtonsoft and System.Text.Json
    /// converters: flattening the object graph into resource pools (identity dedup + uuid assignment),
    /// the node/element type maps, and reference-wiring helpers. Token I/O stays in each converter.
    /// </summary>
    public static class DocumentGraph
    {
        #region Flatten (свёртка)

        public sealed class Pools
        {
            public readonly List<IElement> Geometries = [];
            public readonly List<IElement> Materials = [];
            public readonly List<IElement> Textures = [];
            public readonly List<IElement> Images = [];
            public readonly List<IElement> Fonts = [];

            private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

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

        /// <summary>Walks the graph, assigns missing uuids, and collects the flat resource pools.</summary>
        public static Pools Flatten(Object3D root)
        {
            var pools = new Pools();
            if (root != null)
            {
                Collect(root, pools);
            }
            return pools;
        }

        private static void Collect(Object3D node, Pools pools)
        {
            EnsureUuid(node);

            if (node is IGeometryContainer container && container.Geometry != null)
            {
                EnsureUuid(container.Geometry);
                pools.AddUnique(pools.Geometries, container.Geometry);
            }

            foreach (var material in MaterialsOf(node))
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

        private static void CollectTextures(IMaterial material, Pools pools)
        {
            if (material is MeshStandardMaterial standard)
            {
                foreach (var kvp in standard.GetTextures())
                {
                    AddTexture(kvp.Value, pools);
                }
                return;
            }

            foreach (var (_, prop) in TextureSlots(material.GetType()))
            {
                var texture = (Texture)prop.GetValue(material);
                AddTexture(texture, pools);
            }
        }

        private static void AddTexture(Texture texture, Pools pools)
        {
            if (texture == null)
            {
                return;
            }

            EnsureUuid(texture);
            if (pools.AddUnique(pools.Textures, texture) && texture.Image != null)
            {
                EnsureUuid(texture.Image);
                pools.AddUnique(pools.Images, texture.Image);
            }
        }

        public static IEnumerable<IMaterial> MaterialsOf(Object3D node)
        {
            if (node is Mesh mesh && mesh.Materials != null && mesh.Materials.Count > 0)
            {
                foreach (var material in mesh.Materials)
                {
                    yield return material;
                }
                yield break;
            }

            var single = MaterialOf(node);
            if (single != null)
            {
                yield return single;
            }
        }

        private static IMaterial MaterialOf(Object3D node) => node switch
        {
            Mesh mesh => mesh.Material,
            Line line => line.Material,
            LineSegments segments => segments.Material,
            Points points => points.Material,
            Sprite sprite => sprite.Material,
            _ => null,
        };

        public static void EnsureUuid(IElement element)
        {
            if (element.Uuid == Guid.Empty)
            {
                element.Uuid = Guid.NewGuid();
            }
        }

        #endregion

        #region Reconstruct (развёртка)

        private static readonly Dictionary<string, Type> NodeTypes = BuildTypeMap(typeof(Object3D));
        private static readonly Dictionary<string, Type> ElementTypes = BuildTypeMap(typeof(IElement));

        private static Dictionary<string, Type> BuildTypeMap(Type baseType)
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in typeof(Object3D).Assembly.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface || !baseType.IsAssignableFrom(t))
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

        /// <summary>Concrete node type for a `type` discriminator; falls back to base <see cref="Object3D"/>.</summary>
        public static Object3D CreateNode(string type)
        {
            return (Object3D)Activator.CreateInstance(NodeType(type));
        }

        /// <summary>Concrete <see cref="Object3D"/> type for a node `type`; base <see cref="Object3D"/> if unknown.</summary>
        public static Type NodeType(string type)
        {
            return type != null && NodeTypes.TryGetValue(type, out var concrete) ? concrete : typeof(Object3D);
        }

        /// <summary>Concrete <see cref="IElement"/> type for a pool entry's `type`, or null if unknown.</summary>
        public static Type ElementType(string type)
        {
            return type != null && ElementTypes.TryGetValue(type, out var concrete) ? concrete : null;
        }

        public static void SetMaterial(Object3D obj, IMaterial material)
        {
            switch (obj)
            {
                case Mesh mesh: mesh.Material = material; break;
                case Line line: line.Material = material; break;
                case LineSegments segments: segments.Material = material; break;
                case Points points: points.Material = material; break;
                case Sprite sprite: sprite.Material = material; break;
            }
        }

        public static Dictionary<Guid, IElement> ToUuidMap(IEnumerable<IElement> pool)
        {
            var map = new Dictionary<Guid, IElement>();
            foreach (var element in pool)
            {
                map[element.Uuid] = element;
            }
            return map;
        }

        /// <summary>
        /// Texture slots of a material: JSON key (from the <c>&lt;Name&gt;Uuid</c> property's
        /// <see cref="DataMemberAttribute"/>) paired with the internal <c>&lt;Name&gt;</c> Texture property to set.
        /// </summary>
        public static IEnumerable<(string Key, PropertyInfo Texture)> TextureSlots(Type materialType)
        {
            foreach (var uuidProperty in materialType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (uuidProperty.PropertyType != typeof(Guid?) || !uuidProperty.Name.EndsWith("Uuid", StringComparison.Ordinal))
                {
                    continue;
                }

                var textureName = uuidProperty.Name[..^"Uuid".Length];
                var textureProperty = materialType.GetProperty(textureName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (textureProperty == null || textureProperty.PropertyType != typeof(Texture) || !textureProperty.CanWrite)
                {
                    continue;
                }

                var dataMember = uuidProperty.GetCustomAttribute<DataMemberAttribute>();
                var key = !string.IsNullOrEmpty(dataMember?.Name)
                    ? dataMember.Name
                    : char.ToLowerInvariant(uuidProperty.Name[0]) + uuidProperty.Name[1..];

                yield return (key, textureProperty);
            }
        }

        #endregion
    }
}
