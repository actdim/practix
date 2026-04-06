using ActDim.Practix.Common.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ActDim.Practix.Common.Json;

public class JsonNamingAttributeTests
{
    private readonly Abstractions.Json.IJsonSerializer _ser;

    public JsonNamingAttributeTests()
    {
        _ser = new StandardJsonSerializer();
    }

    // ── Attribute construction ───────────────────────────────────────────────

    [Fact]
    public void Constructor_WithPolicyType_CreatesPolicyInstance()
    {
        var attr = new JsonNamingAttribute(typeof(UpperCaseNamingPolicy));
        Assert.IsAssignableFrom<UpperCaseNamingPolicy>(attr.Policy);
    }

    [Fact]
    public void Constructor_WithNullType_PolicyIsNull()
    {
        var attr = new JsonNamingAttribute(null!);
        Assert.Null(attr.Policy);
    }

    [Fact]
    public void Constructor_WithLowerCasePolicyType_CreatesLowerCaseInstance()
    {
        var attr = new JsonNamingAttribute(typeof(LowerCaseNamingPolicy));
        Assert.IsAssignableFrom<LowerCaseNamingPolicy>(attr.Policy);
    }

    // ── Attribute target / metadata ──────────────────────────────────────────

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        var attrs = typeof(UpperCaseDto).GetCustomAttributes(typeof(JsonNamingAttribute), false);
        Assert.Equal(1, attrs.Length);
    }

    [Fact]
    public void Attribute_CanBeAppliedToStruct()
    {
        var attrs = typeof(LowerCaseStruct).GetCustomAttributes(typeof(JsonNamingAttribute), false);
        Assert.Equal(1, attrs.Length);
    }

    // ── Integration: NamingPolicyResolver через BaseJsonSerializer ───────────

    [Fact]
    public void Serialize_UpperCasePolicy_ProducesUpperCaseKeys()
    {
        var json = _ser.Serialize(new UpperCaseDto { Name = "Alice", Age = 30 });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.True(doc.RootElement.TryGetProperty("NAME", out _));
            Assert.True(doc.RootElement.TryGetProperty("AGE", out _));
        });
    }

    [Fact]
    public void Serialize_LowerCasePolicy_ProducesLowerCaseKeys()
    {
        var json = _ser.Serialize(new LowerCaseDto { FirstName = "Bob", Score = 99 });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.True(doc.RootElement.TryGetProperty("firstname", out _));
            Assert.True(doc.RootElement.TryGetProperty("score", out _));
        });
    }

    [Fact]
    public void Serialize_NullPolicy_KeepsOriginalPropertyNames()
    {
        var json = _ser.Serialize(new NullPolicyDto { MyProp = "x" });
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("MyProp", out _));
    }

    [Fact]
    public void Serialize_ExplicitJsonPropertyName_OverridesNamingPolicy()
    {
        var json = _ser.Serialize(new UpperCaseDtoWithOverride { Name = "Alice", Overridden = "value" });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.True(doc.RootElement.TryGetProperty("NAME", out _));
            Assert.True(doc.RootElement.TryGetProperty("custom_key", out _));
            Assert.False(doc.RootElement.TryGetProperty("OVERRIDDEN", out _));
        });
    }

    [Fact]
    public void Serialize_NoAttribute_UsesNullNamingPolicyByDefault()
    {
        var json = _ser.Serialize(new PlainDto { MyProp = "z" });
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("MyProp", out _));
    }

    [Fact]
    public void Deserialize_UpperCasePolicy_RoundTrip()
    {
        var original = new UpperCaseDto { Name = "Carol", Age = 5 };
        var json = _ser.Serialize(original);
        var restored = _ser.Deserialize<UpperCaseDto>(json);
        Assert.Multiple(() =>
        {
            Assert.Equal("Carol", restored.Name);
            Assert.Equal(5, restored.Age);
        });
    }

    // ── Test DTOs ────────────────────────────────────────────────────────────

    [JsonNaming(typeof(UpperCaseNamingPolicy))]
    private class UpperCaseDto
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [JsonNaming(typeof(UpperCaseNamingPolicy))]
    private class UpperCaseDtoWithOverride
    {
        public string Name { get; set; }

        [JsonPropertyName("custom_key")]
        public string Overridden { get; set; }
    }

    [JsonNaming(typeof(LowerCaseNamingPolicy))]
    private class LowerCaseDto
    {
        public string FirstName { get; set; }
        public int Score { get; set; }
    }

    [JsonNaming(null!)]
    private class NullPolicyDto
    {
        public string MyProp { get; set; }
    }

    private class PlainDto
    {
        public string MyProp { get; set; }
    }

    [JsonNaming(typeof(LowerCaseNamingPolicy))]
    private struct LowerCaseStruct
    {
        public int Value { get; set; }
    }
}
