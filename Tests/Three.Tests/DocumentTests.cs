using System;
using Xunit;
using ActDim.Three;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.NewtonsoftJson;
using ActDim.Three.Objects;

namespace ActDim.Three.Tests
{
	public class Document
	{
		[Fact]
		public void ToSceneDocument_FlattensGraphIntoPools_AndAssignsUuids()
		{
			var geometry = new BufferGeometry();
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1], 3));
			var material = MeshStandardMaterial.Default();

			var scene = new Scene { Name = "s" };
			scene.Add(new Mesh { Geometry = geometry, Material = material, Name = "m" });

			// resources still without a uuid get one assigned during serialization.
			Assert.Equal(Guid.Empty, geometry.Uuid);

			var json = ThreeNewtonsoftSerializer.ToJson(scene.ToSceneDocument());

			Assert.NotEqual(Guid.Empty, geometry.Uuid);
			Assert.NotEqual(Guid.Empty, material.Uuid);

			var doc = ThreeNewtonsoftSerializer.FromJson<SceneDocument>(json);

			// pools reconstructed polymorphically by the type discriminator
			Assert.Single(doc.Geometries);
			Assert.IsType<BufferGeometry>(doc.Geometries[0]);
			Assert.Single(doc.Materials);
			Assert.IsType<MeshStandardMaterial>(doc.Materials[0]);

			Assert.Equal("Scene", doc.Object.Type);
			Assert.Single(doc.Object.Children);
			Assert.Equal("Mesh", doc.Object.Children[0].Type);

			// references resolved to the same pooled instances
			var mesh = Assert.IsType<Mesh>(doc.Object.Children[0]);
			Assert.Same(doc.Geometries[0], mesh.Geometry);
			Assert.Same(doc.Materials[0], mesh.Material);
		}

		[Fact]
		public void Document_IsByteStable_AcrossRoundTrip()
		{
			var geometry = new BufferGeometry();
			geometry.Attributes.Add("position", BufferAttribute.Float32([0, 0, 0, 1, 1, 1, 2, 2, 2], 3));

			var scene = new Scene { Name = "round-trip" };
			var mesh = new Mesh { Geometry = geometry, Material = MeshStandardMaterial.Default(), Name = "m" };
			mesh.Position = new Vector3(1, 2, 3);
			mesh.UpdateMatrix();
			scene.Add(mesh);

			// serialize -> deserialize -> serialize again; the second output must equal the first
			// (uuids assigned once are preserved, never regenerated).
			var json1 = ThreeNewtonsoftSerializer.ToJson(scene.ToSceneDocument());
			var doc = ThreeNewtonsoftSerializer.FromJson<SceneDocument>(json1);
			var json2 = ThreeNewtonsoftSerializer.ToJson(doc);

			Assert.Equal(json1, json2);
		}
	}
}
