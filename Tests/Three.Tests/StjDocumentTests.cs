using System;
using System.Text;
using System.Text.Json;
using Xunit;
using THREE;
using THREE.Core;
using THREE.Core.Buffers;
using THREE.Materials;
using THREE.Objects;

namespace ThreeLib.Tests
{
	public class StjDocument
	{
		private static Scene BuildScene()
		{
			var geometry = new BufferGeometry();
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1, 2, 2, 2], 3));

			var scene = new Scene { Name = "stj" };
			scene.Add(new Mesh { Geometry = geometry, Material = MeshStandardMaterial.Default(), Name = "m" });
			return scene;
		}

		[Fact]
		public void RoundTripsSceneDocument()
		{
			var json1 = JsonSerializer.Serialize(BuildScene().ToSceneDocument());
			var doc = JsonSerializer.Deserialize<SceneDocument>(json1);

			Assert.Equal("Object", doc.Metadata.Type);

			var geometry = Assert.IsType<BufferGeometry>(Assert.Single(doc.Geometries));
			Assert.IsType<Float32Array>(geometry.Attributes["position"].Values);
			Assert.IsType<MeshStandardMaterial>(Assert.Single(doc.Materials));

			var scene = Assert.IsType<Scene>(doc.Object);
			var mesh = Assert.IsType<Mesh>(Assert.Single(scene.Children));
			Assert.Same(doc.Geometries[0], mesh.Geometry);
			Assert.Same(doc.Materials[0], mesh.Material);

			// byte-stable STJ round-trip
			Assert.Equal(json1, JsonSerializer.Serialize(doc));
		}

		[Fact]
		public void StjReadsNewtonsoftOutput()
		{
			// Interop: Newtonsoft writes the document, System.Text.Json reads it back.
			var newtonsoft = Newtonsoft.Json.JsonConvert.SerializeObject(BuildScene().ToSceneDocument());

			var doc = JsonSerializer.Deserialize<SceneDocument>(newtonsoft);

			Assert.IsType<BufferGeometry>(Assert.Single(doc.Geometries));
			Assert.IsType<MeshStandardMaterial>(Assert.Single(doc.Materials));
			var mesh = Assert.IsType<Mesh>(Assert.Single(doc.Object.Children));
			Assert.Same(doc.Geometries[0], mesh.Geometry);
			Assert.Same(doc.Materials[0], mesh.Material);
		}

		[Fact]
		public void SerializerRoundTripsStringAndUtf8()
		{
			var document = BuildScene().ToSceneDocument();

			var json = ThreeSerializer.ToJson(document);
			var fromString = ThreeSerializer.FromJson<SceneDocument>(json);
			Assert.IsType<BufferGeometry>(Assert.Single(fromString.Geometries));
			Assert.IsType<Mesh>(Assert.Single(fromString.Object.Children));

			var utf8 = ThreeSerializer.ToBytes(document);
			ReadOnlyMemory<byte> memory = utf8;
			var fromBytes = ThreeSerializer.FromBytes<SceneDocument>(memory);
			var mesh = Assert.IsType<Mesh>(Assert.Single(fromBytes.Object.Children));
			Assert.Same(fromBytes.Geometries[0], mesh.Geometry);

			// string and UTF-8 paths produce the same JSON
			Assert.Equal(json, Encoding.UTF8.GetString(utf8));
		}

		[Fact]
		public void SerializesArbitraryThreeObject()
		{
			// Not a SceneDocument — an individual geometry round-trips via the shared STJ options.
			var geometry = new BufferGeometry { Uuid = Guid.NewGuid() };
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1], 3));

			var json = ThreeSerializer.ToJson(geometry);
			Assert.Contains("\"data\"", json);
			Assert.Contains("\"position\"", json);

			var back = ThreeSerializer.FromBytes<BufferGeometry>(ThreeSerializer.ToBytes(geometry));
			Assert.IsType<Float32Array>(back.Attributes["position"].Values);
			Assert.Equal(3, back.Attributes["position"].ItemSize);
		}
	}
}
