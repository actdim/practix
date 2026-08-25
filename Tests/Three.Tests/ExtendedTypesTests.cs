using System;
using System.Collections.Generic;
using ActDim.Three.Core;
using ActDim.Three.Core.Buffers;
using ActDim.Three.Geometries;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.NewtonsoftJson;
using ActDim.Three.Objects;
using ActDim.Three.Serialization;
using ActDim.Three.Textures;
using Xunit;

namespace ActDim.Three.Tests
{
    public class ExtendedTypesTests
    {
        [Fact]
        public void Object3D_Layers_BitmaskOperationsWorkCorrectly()
        {
            var node = new Mesh();
            Assert.Equal(1u, node.Layers); // Default is channel 0

            Assert.True(node.IsOnLayer(0));
            Assert.False(node.IsOnLayer(1));
            Assert.False(node.IsOnLayer(2));

            node.EnableLayer(2);
            Assert.True(node.IsOnLayer(0));
            Assert.True(node.IsOnLayer(2));
            Assert.Equal(5u, node.Layers); // 1 | (1 << 2) = 5

            node.DisableLayer(0);
            Assert.False(node.IsOnLayer(0));
            Assert.True(node.IsOnLayer(2));
            Assert.Equal(4u, node.Layers);

            node.ToggleLayer(2);
            Assert.False(node.IsOnLayer(2));
            Assert.Equal(0u, node.Layers);
        }

        [Fact]
        public void ExtendedMaterials_RoundTripThroughStjAndNewtonsoft()
        {
            var scene = new Scene();

            var physicalMat = new MeshPhysicalMaterial
            {
                Color = 0xff0000,
                Clearcoat = 1.0,
                ClearcoatRoughness = 0.1,
                Transmission = 0.8,
                Ior = 1.52
            };
            scene.Add(new Mesh(new BoxGeometry(1, 1, 1), physicalMat) { Name = "PhysicalMesh" });

            var spriteMat = new SpriteMaterial { Color = 0x00ff00, Rotation = 0.78 };
            scene.Add(new Sprite(spriteMat) { Name = "SpriteNode" });

            var toonMat = new MeshToonMaterial { Color = 0x0000ff };
            scene.Add(new Mesh(new SphereGeometry(2), toonMat) { Name = "ToonMesh" });

            var shaderMat = new ShaderMaterial
            {
                VertexShader = "void main() { gl_Position = vec4(position, 1.0); }",
                FragmentShader = "void main() { gl_FragColor = vec4(1.0); }",
                Wireframe = true
            };
            scene.Add(new Mesh(new PlaneGeometry(5, 5), shaderMat) { Name = "ShaderMesh" });

            // 1. System.Text.Json Roundtrip
            var stjJson = ThreeSerializer.ToJson(scene.ToSceneDocument());
            Assert.Contains("MeshPhysicalMaterial", stjJson);
            Assert.Contains("SpriteMaterial", stjJson);
            Assert.Contains("MeshToonMaterial", stjJson);
            Assert.Contains("ShaderMaterial", stjJson);

            var stjDoc = ThreeSerializer.FromJson<SceneDocument>(stjJson);
            Assert.Equal(4, stjDoc.Materials.Count);

            // 2. Newtonsoft.Json Roundtrip
            var newtonJson = ThreeNewtonsoftSerializer.ToJson(scene.ToSceneDocument());
            Assert.Contains("MeshPhysicalMaterial", newtonJson);
            Assert.Contains("SpriteMaterial", newtonJson);

            var newtonDoc = ThreeNewtonsoftSerializer.FromJson<SceneDocument>(newtonJson);
            Assert.Equal(4, newtonDoc.Materials.Count);
        }

        [Fact]
        public void ExtendedNodes_InstancedMeshAndLOD_RoundTrip()
        {
            var scene = new Scene();

            var instancedGeo = new InstancedBufferGeometry { InstanceCount = 500 };
            instancedGeo.Attributes["position"] = BufferAttribute.Float32(new float[] { 0, 0, 0, 1, 1, 1 }, 3);

            var instancedMesh = new InstancedMesh(instancedGeo, new MeshStandardMaterial { Color = 0xaaaaaa }, 500)
            {
                Name = "MyInstancedMesh",
                InstanceMatrix = BufferAttribute.Float32(new float[500 * 16], 16)
            };
            scene.Add(instancedMesh);

            var lodNode = new LOD { Name = "MyLOD" };
            lodNode.AddLevel(new Mesh(new BoxGeometry(10, 10, 10), new MeshBasicMaterial()), distance: 0);
            lodNode.AddLevel(new Mesh(new BoxGeometry(1, 1, 1), new MeshBasicMaterial()), distance: 100);
            scene.Add(lodNode);

            var stjJson = ThreeSerializer.ToJson(scene.ToSceneDocument());
            Assert.Contains("InstancedMesh", stjJson);
            Assert.Contains("InstancedBufferGeometry", stjJson);
            Assert.Contains("LOD", stjJson);

            var doc = ThreeSerializer.FromJson<SceneDocument>(stjJson);
            Assert.NotNull(doc.Object);
        }

        [Fact]
        public void InterleavedBuffer_StructureAndSerialization()
        {
            var interleaved = new InterleavedBuffer(new Float32Array { Data = new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 1, 0 } }, stride: 6);
            Assert.Equal(6, interleaved.Stride);

            var posAttr = new InterleavedBufferAttribute(interleaved, itemSize: 3, offset: 0);
            var normalAttr = new InterleavedBufferAttribute(interleaved, itemSize: 3, offset: 3);

            Assert.Equal(3, posAttr.ItemSize);
            Assert.Equal(0, posAttr.Offset);
            Assert.Equal(3, normalAttr.ItemSize);
            Assert.Equal(3, normalAttr.Offset);
        }
    }
}
