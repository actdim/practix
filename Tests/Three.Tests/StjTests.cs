using System.Text.Json;
using Xunit;
using ActDim.Three.Core;
using ActDim.Three.Materials;
using ActDim.Three.Serialization;

namespace ActDim.Three.Tests
{
	public class Stj
	{
		private static readonly JsonSerializerOptions Options = new()
        {
			TypeInfoResolver = DataContractResolver.Instance,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		};

		[Fact]
		public void HonorsDataMemberNames()
		{
			var json = JsonSerializer.Serialize(MeshStandardMaterial.Default(), Options);

			// explicit [DataMember(Name=...)] names, not PascalCase
			Assert.Contains("\"color\"", json);
			Assert.Contains("\"roughness\"", json);
			Assert.Contains("\"metalness\"", json);
			Assert.DoesNotContain("\"Color\"", json);
			Assert.DoesNotContain("\"Roughness\"", json);
		}

		[Fact]
		public void OptIn_ExcludesBaseMembersWithoutDataMember()
		{
			var json = JsonSerializer.Serialize(MeshStandardMaterial.Default(), Options);

			// MeshStandardMaterial is [DataContract] (opt-in); Material base props lack [DataMember].
			Assert.DoesNotContain("alphaTest", json);
			Assert.DoesNotContain("depthTest", json);
		}

		[Fact]
		public void RespectsIgnoreDataMember()
		{
			var child = new Object3D { Name = "child" };
			var parent = new Object3D { Name = "parent" };
			parent.Add(child); // sets child.Parent = parent

			var json = JsonSerializer.Serialize(child, Options);

			// Parent is [IgnoreDataMember] -> excluded (also avoids a cycle)
			Assert.DoesNotContain("\"parent\"", json.ToLowerInvariant());
			Assert.Contains("\"castShadow\"", json);
		}
	}
}
