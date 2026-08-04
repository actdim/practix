using Newtonsoft.Json;
using Xunit;
using ActDim.Three;
using ActDim.Three.Core;
using ActDim.Three.Core.Buffers;
using ActDim.Three.Materials;
using ActDim.Three.Objects;

namespace ActDim.Three.Tests
{
	public class GeometryData
	{
		[Fact]
		public void RoundTripsGroupsDrawRangeAndMorphAttributes()
		{
			var geometry = new BufferGeometry();
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1, 2, 2, 2], 3));
			geometry.Groups.Add(new GeometryGroup { Start = 0, Count = 3, MaterialIndex = 0 });
			geometry.Groups.Add(new GeometryGroup { Start = 3, Count = 3, MaterialIndex = 1 });
			geometry.DrawRange = new DrawRange { Start = 0, Count = 6 };
			geometry.MorphAttributes["position"] = [BufferAttribute.Float32([0.1f, 0, 0, 0, 0.1f, 0, 0, 0, 0.1f], 3)];

			var scene = new Scene();
			scene.Add(new Mesh { Geometry = geometry, Material = MeshStandardMaterial.Default() });

			var json1 = JsonConvert.SerializeObject(scene.ToSceneDocument());
			var doc = JsonConvert.DeserializeObject<SceneDocument>(json1);

			var geo = Assert.IsType<BufferGeometry>(doc.Geometries[0]);

			Assert.Equal(2, geo.Groups.Count);
			Assert.Equal(3, geo.Groups[1].Start);
			Assert.Equal(1, geo.Groups[1].MaterialIndex);

			Assert.NotNull(geo.DrawRange);
			Assert.Equal(6, geo.DrawRange.Count);

			Assert.True(geo.MorphAttributes.ContainsKey("position"));
			var morph = Assert.Single(geo.MorphAttributes["position"]);
			Assert.IsType<Float32Array>(morph.Values);

			// byte-stable round-trip
			Assert.Equal(json1, JsonConvert.SerializeObject(doc));
		}

		[Fact]
		public void RoundTripsMultiMaterialMesh()
		{
			var geometry = new BufferGeometry();
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1, 2, 2, 2], 3));

			var mesh = new Mesh { Geometry = geometry };
			mesh.Materials.Add(MeshStandardMaterial.Default());
			mesh.Materials.Add(new MeshBasicMaterial());

			var scene = new Scene();
			scene.Add(mesh);

			var json = JsonConvert.SerializeObject(scene.ToSceneDocument());
			Assert.Contains("\"material\":[", json); // emitted as an array

			var doc = JsonConvert.DeserializeObject<SceneDocument>(json);
			Assert.Equal(2, doc.Materials.Count);

			var readMesh = Assert.IsType<Mesh>(doc.Object.Children[0]);
			Assert.Equal(2, readMesh.Materials.Count);
			Assert.Same(doc.Materials[0], readMesh.Materials[0]);
			Assert.Same(doc.Materials[1], readMesh.Materials[1]);
		}
	}
}
