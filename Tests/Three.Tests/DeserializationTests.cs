using Xunit;
using THREE;
using THREE.Core;
using THREE.Core.Buffers;
using THREE.Materials;
using THREE.Objects;

namespace ThreeLib.Tests
{
	public class Deserialization
	{
		// A three.js "Object" scene, as produced by ThreeLib-Object3D.toJSON:
		// a Scene -> Group -> Mesh referencing one BufferGeometry and one MeshStandardMaterial.
		private const string Object3DJson = """
		{"metadata":{"version":4.5,"type":"Object","generator":"ThreeLib-Object3D.toJSON"},"geometries":[{"data":{"attributes":{"position":{"uuid":"2e3d83b2-d3a2-40e2-b3c7-7c58735e3d2c","name":"position","itemSize":3,"count":36,"type":"Float32Array","array":[-5.0588,-6.5926,0.4571,-5.0588,-6.5926,0.4671,-5.0714,-6.6082,0.4671,-5.0588,-6.5926,0.4571,-5.0714,-6.6082,0.4671,-5.0714,-6.6082,0.4571,-5.0714,-6.6082,0.4571,-5.0714,-6.6082,0.4671,-4.9077,-6.7398,0.4671,-5.0714,-6.6082,0.4571,-4.9077,-6.7398,0.4671,-4.9077,-6.7398,0.4571,-4.9077,-6.7398,0.4571,-4.9077,-6.7398,0.4671,-4.8952,-6.7242,0.4671,-4.9077,-6.7398,0.4571,-4.8952,-6.7242,0.4671,-4.8952,-6.7242,0.4571,-4.8952,-6.7242,0.4571,-4.8952,-6.7242,0.4671,-5.0588,-6.5926,0.4671,-4.8952,-6.7242,0.4571,-5.0588,-6.5926,0.4671,-5.0588,-6.5926,0.4571,-5.0588,-6.5926,0.4671,-4.8952,-6.7242,0.4671,-4.9077,-6.7398,0.4671,-5.0588,-6.5926,0.4671,-4.9077,-6.7398,0.4671,-5.0714,-6.6082,0.4671,-4.8952,-6.7242,0.4571,-5.0588,-6.5926,0.4571,-5.0714,-6.6082,0.4571,-4.8952,-6.7242,0.4571,-5.0714,-6.6082,0.4571,-4.9077,-6.7398,0.4571]},"colorCompact":{"uuid":"500ae1ab-074d-4a39-bbee-3fa2ac9699d7","name":"colorCompact","itemSize":1,"count":36,"type":"Uint32Array","array":[16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215,16777215]},"id":{"uuid":"55f8859e-2bb8-446c-9f42-4526d880f2d4","name":"id","itemSize":1,"count":36,"type":"Uint32Array","array":[6601,6601,6601,6602,6602,6602,6603,6603,6603,6604,6604,6604,6605,6605,6605,6606,6606,6606,6607,6607,6607,6608,6608,6608,6609,6609,6609,6610,6610,6610,6611,6611,6611,6612,6612,6612]}}},"uuid":"25ed225c-396c-486a-a52f-ef28f8b39fd6","type":"BufferGeometry"}],"materials":[{"uuid":"5547acca-f070-495a-a0b5-7c89b55a5e37","type":"MeshStandardMaterial"}],"object":{"background":16777215,"children":[{"children":[{"geometry":"25ed225c-396c-486a-a52f-ef28f8b39fd6","material":"5547acca-f070-495a-a0b5-7c89b55a5e37","children":[],"matrix":[1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0],"uuid":"3680d032-861c-4d44-a752-a94368706763","name":"6b6cd7c2-d0c6-4a34-8773-d611f7f7f9bc","type":"Mesh"}],"matrix":[1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0],"uuid":"e89b99d9-a6b5-4333-a1c0-126711f33bd1","type":"Group"}],"matrix":[1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0],"uuid":"16e78082-8839-4eb5-bf59-f0e2ee56ab36","name":"1145e8a6-39ba-4710-b4b8-86bc799ed278","type":"Scene"},"fonts":[]}
		""";

		[Fact]
		public void CanDeserializeObject3D()
		{
			var adapter = ThreeJson.Deserialize(Object3DJson);

			Assert.NotNull(adapter);

			// metadata
			Assert.Equal(4.5, adapter.Metadata.Version);
			Assert.Equal("Object", adapter.Metadata.Type);
			Assert.Equal("ThreeLib-Object3D.toJSON", adapter.Metadata.Generator);

			// geometries
			Assert.Single(adapter.Geometries);
			var geometry = Assert.IsType<BufferGeometry>(adapter.Geometries[0]);
			Assert.Equal("25ed225c-396c-486a-a52f-ef28f8b39fd6", geometry.Uuid.ToString());
			Assert.True(geometry.Attributes.ContainsKey("position"));
			Assert.Equal(3, geometry.Attributes["position"].ItemSize);
			Assert.Equal("Float32Array", geometry.Attributes["position"].Type);

			// no-boxing guard: the payload is a typed float[] buffer, never object[].
			var position = Assert.IsType<Float32Array>(geometry.Attributes["position"].Values);
			Assert.IsType<float[]>(position.Data);
			Assert.Equal(36 * 3, position.Data.Length);

			// materials
			Assert.Single(adapter.Materials);
			var material = Assert.IsType<MeshStandardMaterial>(adapter.Materials[0]);
			Assert.Equal("5547acca-f070-495a-a0b5-7c89b55a5e37", material.Uuid.ToString());

			// object graph rebuilt into CONCRETE node types: Scene -> Group -> Mesh
			var scene = Assert.IsType<Scene>(adapter.Object);
			Assert.Equal("16e78082-8839-4eb5-bf59-f0e2ee56ab36", scene.Uuid.ToString());
			Assert.Equal("1145e8a6-39ba-4710-b4b8-86bc799ed278", scene.Name);
			var group = Assert.IsType<Group>(Assert.Single(scene.Children));
			var mesh = Assert.IsType<Mesh>(Assert.Single(group.Children));
			Assert.Equal("3680d032-861c-4d44-a752-a94368706763", mesh.Uuid.ToString());
			Assert.Equal(scene, group.Parent); // Parent wired
			Assert.Equal(group, mesh.Parent);

			// uuid references resolved back to the pooled objects (same instances)
			Assert.Same(adapter.Geometries[0], mesh.Geometry);
			Assert.Same(adapter.Materials[0], mesh.Material);
		}
	}
}
