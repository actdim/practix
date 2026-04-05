using ActDim.Practix.Service.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Tests.Json;

[TestFixture]
public class JsonNamingAttributeTests
{
    private Abstractions.Json.IJsonSerializer _ser;

    [SetUp]
    public void SetUp()
    {
        _ser = new StandardJsonSerializer();
    }

    // ── Attribute construction ───────────────────────────────────────────────

    [Test]
    public void Constructor_WithPolicyType_CreatesPolicyInstance()
    {
        var attr = new JsonNamingAttribute(typeof(UpperCaseNamingPolicy));
        Assert.That(attr.Policy, Is.InstanceOf<UpperCaseNamingPolicy>());
    }

    [Test]
    public void Constructor_WithNullType_PolicyIsNull()
    {
        var attr = new JsonNamingAttribute(null!);
        Assert.That(attr.Policy, Is.Null);
    }

    [Test]
    public void Constructor_WithLowerCasePolicyType_CreatesLowerCaseInstance()
    {
        var attr = new JsonNamingAttribute(typeof(LowerCaseNamingPolicy));
        Assert.That(attr.Policy, Is.InstanceOf<LowerCaseNamingPolicy>());
    }

    // ── Attribute target / metadata ──────────────────────────────────────────

    [Test]
    public void Attribute_CanBeAppliedToClass()
    {
        var attrs = typeof(UpperCaseDto).GetCustomAttributes(typeof(JsonNamingAttribute), false);
        Assert.That(attrs, Has.Length.EqualTo(1));
    }

    [Test]
    public void Attribute_CanBeAppliedToStruct()
    {
        var attrs = typeof(LowerCaseStruct).GetCustomAttributes(typeof(JsonNamingAttribute), false);
        Assert.That(attrs, Has.Length.EqualTo(1));
    }

    // ── Integration: NamingPolicyResolver через BaseJsonSerializer ───────────

    [Test]
    public void Serialize_UpperCasePolicy_ProducesUpperCaseKeys()
    {
        var json = _ser.Serialize(new UpperCaseDto { Name = "Alice", Age = 30 });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.TryGetProperty("NAME", out _), Is.True);
            Assert.That(doc.RootElement.TryGetProperty("AGE", out _), Is.True);
        });
    }

    [Test]
    public void Serialize_LowerCasePolicy_ProducesLowerCaseKeys()
    {
        var json = _ser.Serialize(new LowerCaseDto { FirstName = "Bob", Score = 99 });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.TryGetProperty("firstname", out _), Is.True);
            Assert.That(doc.RootElement.TryGetProperty("score", out _), Is.True);
        });
    }

    [Test]
    public void Serialize_NullPolicy_KeepsOriginalPropertyNames()
    {
        var json = _ser.Serialize(new NullPolicyDto { MyProp = "x" });
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("MyProp", out _), Is.True);
    }

    [Test]
    public void Serialize_ExplicitJsonPropertyName_OverridesNamingPolicy()
    {
        var json = _ser.Serialize(new UpperCaseDtoWithOverride { Name = "Alice", Overridden = "value" });
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.TryGetProperty("NAME", out _), Is.True, "policy applied to Name");
            Assert.That(doc.RootElement.TryGetProperty("custom_key", out _), Is.True, "explicit name wins");
            Assert.That(doc.RootElement.TryGetProperty("OVERRIDDEN", out _), Is.False, "policy NOT applied");
        });
    }

    [Test]
    public void Serialize_NoAttribute_UsesNullNamingPolicyByDefault()
    {
        var json = _ser.Serialize(new PlainDto { MyProp = "z" });
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("MyProp", out _), Is.True);
    }

    [Test]
    public void Deserialize_UpperCasePolicy_RoundTrip()
    {
        var original = new UpperCaseDto { Name = "Carol", Age = 5 };
        var json = _ser.Serialize(original);
        var restored = _ser.Deserialize<UpperCaseDto>(json);
        Assert.Multiple(() =>
        {
            Assert.That(restored.Name, Is.EqualTo("Carol"));
            Assert.That(restored.Age, Is.EqualTo(5));
        });
    }

    // ── Test DTOs ────────────────────────────────────────────────────────────

    [JsonNaming(typeof(UpperCaseNamingPolicy))]
    private class UpperCaseDto
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    [JsonNaming(typeof(UpperCaseNamingPolicy))]
    private class UpperCaseDtoWithOverride
    {
        public string? Name { get; set; }

        [JsonPropertyName("custom_key")]
        public string? Overridden { get; set; }
    }

    [JsonNaming(typeof(LowerCaseNamingPolicy))]
    private class LowerCaseDto
    {
        public string? FirstName { get; set; }
        public int Score { get; set; }
    }

    [JsonNaming(null!)]
    private class NullPolicyDto
    {
        public string? MyProp { get; set; }
    }

    private class PlainDto
    {
        public string? MyProp { get; set; }
    }

    [JsonNaming(typeof(LowerCaseNamingPolicy))]
    private struct LowerCaseStruct
    {
        public int Value { get; set; }
    }
}
