using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using ActDim.Three.Core;
using ActDim.Three.Geometries;
using ActDim.Three.Lights;
using ActDim.Three.Materials;
using ActDim.Three.Math;
using ActDim.Three.Objects;

namespace ActDim.Three.Tests
{
	public class Common
	{
		[Fact]
		public void CanSerializeComplexScene()
		{
			var scene = new Scene
			{
				Background = new Color(255, 0, 255).ToInt(),
				Name = "My Scene"
			};

			List<float[]> verts =
			[
				[0, 0, 0],
				[0, 0, 10.1234f],
				[10, 0, 10],
				[10, 0, 0]
			];

			List<float[]> norms =
			[
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0]
			];

			var vertices = verts.SelectMany(v => v).ToList(); // Geometry.ProcessVertexArray(verts);

			var normals = norms.SelectMany(v => v).ToList();

			int[] face1 = [0, 1, 2];
			int[] face2 = [0, 2, 3];

			var faces = Geometry.ProcessFaceArray([face1, face2], false, false);

			var geometry = new Geometry(vertices, faces, normals);
			var geometry2 = new Geometry(vertices, faces, normals);
			var material = MeshStandardMaterial.Default();

			var mesh = new Mesh
			{
				Geometry = geometry2,
				Material = material,
				Name = "My Mesh"
			};

			scene.Add(mesh);

			var material2 = MeshStandardMaterial.Default();
			material2.Roughness = 0.25;

			var mesh2 = new Mesh
			{
				Geometry = geometry,
				Material = material2,
				Position = new Vector3(20, 20, 20),
				Name = "My Mesh2"
			};

			scene.Add(mesh2);

			var material3 = MeshStandardMaterial.Default();

			var mesh3 = new Mesh
			{
				Geometry = geometry2,
				Material = material3,
				Position = new Vector3(30, 30, 30),
				Name = "My Mesh3"
			};

			scene.Add(mesh3);

			var line = new Line
			{
				Geometry = new Geometry(vertices),
				Material = new LineBasicMaterial { Color = new Color(255, 0, 0).ToInt(), LineWidth = 20 },
				Name = "My Curves"
			};

			scene.Add(line);

			List<int> colors =
			[
				255, 0, 0,
				255, 255, 0,
				255, 0, 255,
				0, 255, 0
			];

			var pointsGeometry = new BufferGeometry
			{
				Attributes =
				{
					{ "position", new BufferAttribute("Float32Array", vertices.Cast<object>().ToArray(), 3) },
					{ "color", new BufferAttribute("Uint8Array", colors.Cast<object>().ToArray(), 3) }
				},
				BoundingSphere = new BufferGeometryBoundingSphere
				{
					Center = [0, 0, 0],
					Radius = 4
				}
			};

			var points = new Points
			{
				Geometry = pointsGeometry,
				Material = new PointsMaterial { VertexColors = VertexColors.Vertex, Size = 10 },
				Name = "My Points"
			};

			scene.Add(points);

			var points2 = new Points
			{
				Geometry = pointsGeometry,
				Material = new PointsMaterial { VertexColors = VertexColors.Vertex, Size = 10 },
				Name = "My Points2"
			};

			scene.Add(points2);

			List<float[]> verts2 =
			[
				[0, 0, 0],
				[0, 0, 10],
				[10, 0, 10],
				[0, 0, 0],
				[10, 0, 10],
				[10, 0, 0]
			];

			List<float[]> norms2 =
			[
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0],
				[0, 1, 0]
			];

			List<float[]> color2 =
			[
				[0, 0, 255],
				[0, 0, 255],
				[0, 0, 255],
				[255, 0, 0],
				[255, 0, 0],
				[255, 0, 0],
			];

			List<float[]> uv2 =
			[
				[0, 0],
				[1, 0.5f],
				[1, 0],
				[0, 0],
				[0.5f, 1],
				[1, 0.5f]
			];

			var meshBG = new BufferGeometry
			{
				Attributes =
				{
					{ "position", new BufferAttribute("Float32Array", verts2.SelectMany(v => v).ToArray(), 3) },
					{ "normal", new BufferAttribute("Float32Array", norms2.SelectMany(v => v).ToArray(), 3) },
					{ "uv", new BufferAttribute("Float32Array", uv2.SelectMany(v => v).ToArray(), 2) },
					{ "color", new BufferAttribute("Float32Array", color2.SelectMany(v => v).ToArray(), 3) }

				},
				BoundingSphere = new BufferGeometryBoundingSphere
				{
					Center = [0, 0, 0],
					Radius = 5
				}
			};

			var mesh6 = new Mesh
			{
				Geometry = meshBG,
				Material = MeshStandardMaterial.Default(),
				Name = "MeshfromBufferGeo"
			};

			(mesh6.Material as MeshStandardMaterial).VertexColors = VertexColors.Vertex;

			scene.Add(mesh6);

			object[] verts3 =
			[
				0, 0, 0,
				0, 0, 10,
				10, 0, 10,
				10, 0, 0
			];

			object[] indexes = [0, 1, 2, 0, 2, 3];

			object[] norms3 =
			[
				0, 1, 0,
				0, 1, 0,
				0, 1, 0,
				0, 1, 0,
				0, 1, 0,
				0, 1, 0
			];

			object[] color3 =
			[
				0, 0, 255,
				0, 0, 255,
				0, 0, 255,
				255, 0, 0,
				255, 0, 0,
				255, 0, 0,
			];

			object[] uv3 =
			[
				0, 0,
				1, 0.5,
				1, 0,
				0, 0,
				0.5, 1,
				1, 0.5
			];

			var meshIBG = new BufferGeometry
			{
				Attributes =
				{
					{ "position", new BufferAttribute("Float32Array", verts3, 3) },
					{ "index", new BufferAttribute("Uint32Array", indexes, 1) },
					{ "normal", new BufferAttribute("Float32Array", norms3, 3) },
					{ "uv", new BufferAttribute("Float32Array", uv3, 2) },
					{ "color", new BufferAttribute("Float32Array", color3, 3) }

				},
				BoundingSphere = new BufferGeometryBoundingSphere
				{
					Center = [0, 0, 0],
					Radius = 5
				}
			};

			var mesh7 = new Mesh
			{
				Geometry = meshIBG,
				Material = MeshStandardMaterial.Default(),
				Name = "MeshfromIndexedBufferGeo"
			};

			(mesh7.Material as MeshStandardMaterial).VertexColors = VertexColors.Vertex;

			scene.Add(mesh7);

			var mesh4 = new Mesh
			{
				Geometry = geometry2,
				Material = material3,
				Position = new Vector3(30, 30, 30),
				Name = "My Mesh4"
			};

			var sphereGeoAsChild = new SphereGeometry(3, 22, 22);

			var sphereMeshAsChild = new Mesh
			{
				Geometry = sphereGeoAsChild,
				Material = material,
				Position = new Vector3(-45, 10, 45),
				Name = "My Sphere as a Child"
			};

			mesh4.Add(sphereMeshAsChild);
			scene.Add(mesh4);

			var group = new Group();

			group.Add(mesh3);
			group.Add(mesh2);
			group.Add(mesh);

			scene.Add(group);

			var group2 = new Group();

			group2.Add(mesh3);
			group2.Add(mesh2);
			group2.Add(mesh);

			scene.Add(group2);

			var sphereGeometry = new SphereGeometry(10, 10, 5);

			var sphereMesh = new Mesh
			{
				Geometry = sphereGeometry,
				Material = material,
				Position = new Vector3(-45, 10, 45),
				Name = "My Sphere"
			};

			scene.Add(sphereMesh);

			var pointLight = new PointLight
			{
				Color = new Color(100, 100, 100).ToInt(),
				Decay = 1,
				Intensity = 3,
				Name = "My PointLight",
				Position = new Vector3(10, 10, 10)
			};

			scene.Add(pointLight);

			var ambientLight = new AmbientLight
			{
				Color = new Color(255, 0, 255).ToInt(),
				Intensity = 5,
				Name = "My AmbientLight"
			};

			scene.Add(ambientLight);

			var targetObject = new Object3D { Position = new Vector3(3, 0, 0) };
			scene.Add(targetObject);

			var directionalLight = new DirectionalLight
			{
				Target = targetObject,
				Position = new Vector3(-10, 10, 5),
				Name = "My DirectionalLight"
			};

			scene.Add(directionalLight);

			var spotLight = new SpotLight
			{
				Target = new Object3D { Position = new Vector3(3, 0, 3) },
				Position = new Vector3(20, 20, 0),
				Name = "My SpotLight"
			};

			scene.Add(spotLight);

			var hemiLight = new HemisphereLight
			{
				SkyColor = new Color(0, 30, 255).ToInt(),
				GroundColor = new Color(30, 30, 30).ToInt(),
				Name = "My HemisphereLight"
			};

			scene.Add(hemiLight);

			var json = JsonConvert.SerializeObject(scene.ToSceneDocument());
			Assert.False(string.IsNullOrEmpty(json));
		}
	}
}
