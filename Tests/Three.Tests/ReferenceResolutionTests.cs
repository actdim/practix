using System.Linq;
using Newtonsoft.Json;
using Xunit;
using ActDim.Three;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Objects;
using ActDim.Three.Textures;

namespace ActDim.Three.Tests
{
	public class ReferenceResolution
	{
		// Scene -> Mesh -> (BufferGeometry) + (MeshStandardMaterial -> map texture -> image).
		private const string TexturedJson = """
		{"metadata":{"version":4.5,"type":"Object","generator":"test"},
		"geometries":[{"data":{"attributes":{"position":{"itemSize":3,"type":"Float32Array","array":[0,0,0,1,1,1,2,2,2]}}},"uuid":"11111111-1111-1111-1111-111111111111","type":"BufferGeometry"}],
		"materials":[{"uuid":"22222222-2222-2222-2222-222222222222","type":"MeshStandardMaterial","map":"33333333-3333-3333-3333-333333333333"}],
		"textures":[{"uuid":"33333333-3333-3333-3333-333333333333","image":"44444444-4444-4444-4444-444444444444"}],
		"images":[{"uuid":"44444444-4444-4444-4444-444444444444","url":"data:image/png;base64,AAAA"}],
		"object":{"uuid":"55555555-5555-5555-5555-555555555555","type":"Scene","matrix":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1],"children":[{"uuid":"66666666-6666-6666-6666-666666666666","type":"Mesh","geometry":"11111111-1111-1111-1111-111111111111","material":"22222222-2222-2222-2222-222222222222","matrix":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]}]}}
		""";

		[Fact]
		public void ResolvesMaterialTextureAndImageReferences()
		{
			var doc = JsonConvert.DeserializeObject<SceneDocument>(TexturedJson);

			Assert.IsType<MeshStandardMaterial>(doc.Materials[0]);
			Assert.IsType<Texture>(Assert.Single(doc.Textures));
			Assert.IsType<Image>(Assert.Single(doc.Images));

			// Re-serializing rebuilds the pools from the graph: the texture survives only if the mesh's
			// material resolved its `map` reference (material -> texture), and the image survives only if
			// that texture resolved its `image` reference (texture -> image). So non-empty pools after a
			// round-trip prove both references were wired on read.
			var roundTrip = JsonConvert.DeserializeObject<SceneDocument>(JsonConvert.SerializeObject(doc));
			Assert.Single(roundTrip.Textures);
			Assert.Single(roundTrip.Images);
		}

		// Legacy (non-buffer) Geometry with inline vertices/normals.
		private const string LegacyGeometryJson = """
		{"metadata":{"version":4.5,"type":"Object","generator":"test"},
		"geometries":[{"data":{"vertices":[0,0,0,1,2,3],"normals":[0,1,0,0,1,0],"colors":[],"faces":[],"uvs":[]},"uuid":"11111111-1111-1111-1111-111111111111","type":"Geometry"}],
		"materials":[{"uuid":"22222222-2222-2222-2222-222222222222","type":"MeshStandardMaterial"}],
		"object":{"uuid":"55555555-5555-5555-5555-555555555555","type":"Scene","matrix":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1],"children":[{"uuid":"66666666-6666-6666-6666-666666666666","type":"Mesh","geometry":"11111111-1111-1111-1111-111111111111","material":"22222222-2222-2222-2222-222222222222","matrix":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]}]}}
		""";

		[Fact]
		public void ReadsLegacyGeometryVerticesAndNormals()
		{
			var doc = JsonConvert.DeserializeObject<SceneDocument>(LegacyGeometryJson);

			var geometry = Assert.IsType<Geometry>(doc.Geometries[0]);
			Assert.Equal([0f, 0f, 0f, 1f, 2f, 3f], geometry.Vertices);
			Assert.Equal([0f, 1f, 0f, 0f, 1f, 0f], geometry.Normals);
		}
	}
}
