using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Service.Json;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Tests.Json;

[TestFixture]
public class StandardJsonSerializerTests
{
    private StandardJsonSerializer _ser;

    [SetUp]
    public void SetUp()
    {
        _ser = new StandardJsonSerializer();
    }

    private JsonMergeOptions MergeOptsFor(JsonMergeArrayHandling arrayHandling) => new()
    {
        BaseOptions = _ser.CreateDefaultOptions(),
        MergeArrayHandling = arrayHandling,
        MergeNullValueHandling = JsonMergeNullValueHandling.Merge
    };

    private static List<int> ParseItemsArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return [.. doc.RootElement.GetProperty("Items").EnumerateArray().Select(x => x.GetInt32())];
    }

    // ── DefaultOptions ───────────────────────────────────────────────────────

    [Test]
    public void GetDefaultOptions_ReturnsNullNamingPolicy()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.That(options.PropertyNamingPolicy, Is.Null);
    }

    [Test]
    public void GetDefaultOptions_ReturnsPropertyNameCaseInsensitive()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.That(options.PropertyNameCaseInsensitive, Is.True);
    }

    [Test]
    public void GetDefaultOptions_NeverIgnoresProperties()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.That(options.DefaultIgnoreCondition, Is.EqualTo(JsonIgnoreCondition.Never));
    }

    [Test]
    public void GetDefaultOptions_NotIndented()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.That(options.WriteIndented, Is.False);
    }

    // ── GetDefaultMergeOptions ───────────────────────────────────────────────

    [Test]
    public void GetDefaultMergeOptions_ReturnsReplaceArrayHandling()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.That(options.MergeArrayHandling, Is.EqualTo(JsonMergeArrayHandling.Replace));
    }

    [Test]
    public void GetDefaultMergeOptions_ReturnsMergeNullValueHandling()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.That(options.MergeNullValueHandling, Is.EqualTo(JsonMergeNullValueHandling.Merge));
    }

    [Test]
    public void GetDefaultMergeOptions_HasBaseOptions()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.That(options.BaseOptions, Is.Not.Null);
    }

    // ── SerializeObject ──────────────────────────────────────────────────────

    [Test]
    public void SerializeObject_ProducesValidJson()
    {
        var result = _ser.Serialize(new { Name = "Alice" });
        Assert.That(result, Is.EqualTo("{\"Name\":\"Alice\"}"));
        result = _ser.Serialize("test");
        Assert.That(result, Is.EqualTo("\"test\""));
        result = _ser.Serialize(99);
        Assert.That(result, Is.EqualTo("99"));
    }

    [Test]
    public void SerializeObject_IncludesNullProperties()
    {
        var result = _ser.Serialize(new { Name = (string?)null, Age = 5 });
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("\"Name\":null"));
            Assert.That(result, Does.Contain("Age"));
        });
    }

    [Test]
    public void SerializeObject_Enum_DefaultsToInteger()
    {
        // Without JsonStringEnumConverter in options, enum serializes as integer
        var result = _ser.Serialize(new { Kind = JsonMergeArrayHandling.Concat });
        Assert.That(result, Is.EqualTo("{\"Kind\":0}"));
    }

    [Test]
    public void SerializeObject_Enum_AsString_WithExplicitConverter()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Serialize(new
        {
            JsonMergeArrayHandling.Concat,
            JsonMergeArrayHandling.Replace,
            JsonMergeArrayHandling.Merge,
            JsonMergeArrayHandling.Union
        }, options);
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("\"Concat\""));
            Assert.That(result, Does.Contain("\"Replace\""));
            Assert.That(result, Does.Contain("\"Merge\""));
            Assert.That(result, Does.Contain("\"Union\""));
        });
    }

    [Test]
    public void SerializeObject_DateTimeFormat()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 45, 123, DateTimeKind.Unspecified);
        var result = _ser.Serialize(new { When = dt });
        Assert.That(result, Does.Contain("\"2024-06-15T10:30:45.123\""));
    }

    [Test]
    public void SerializeObject_WithExplicitOptions_UsesProvidedOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = _ser.Serialize(new { X = 1 }, options);
        Assert.That(result, Does.Contain(Environment.NewLine));
    }

    [Test]
    public void SerializeObject_WithNullOptions_FallsBackToDefault()
    {
        var result = _ser.Serialize(new { Name = "X" }, (JsonSerializerOptions?)null);
        Assert.That(result, Is.EqualTo("{\"Name\":\"X\"}"));
    }

    // ── SerializeObject (stream) ─────────────────────────────────────────────

    [Test]
    public void SerializeObject_ToStream_WritesJson()
    {
        using var ms = new MemoryStream();
        _ser.Serialize(new { Value = 42 }, ms);
        var json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.That(json, Is.EqualTo("{\"Value\":42}"));
    }

    [Test]
    public async Task SerializeObjectAsync_ToStream_WritesJson()
    {
        using var ms = new MemoryStream();
        await _ser.SerializeAsync(new { Value = 99 }, ms);
        var json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.That(json, Is.EqualTo("{\"Value\":99}"));
    }

    // ── DeserializeObject ────────────────────────────────────────────────────

    private record PersonRecord(string Name, int Age);

    [Test]
    public void DeserializeObject_Generic_ReturnsTypedObject()
    {
        var result = _ser.Deserialize<PersonRecord>("{\"name\":\"Bob\",\"age\":30}");
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Bob"));
            Assert.That(result.Age, Is.EqualTo(30));
        });
    }

    [Test]
    public void DeserializeObject_NonGeneric_ReturnsObject()
    {
        var result = _ser.Deserialize("{\"name\":\"Carol\"}", typeof(PersonRecord));
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<PersonRecord>());
            Assert.That(((PersonRecord)result).Name, Is.EqualTo("Carol"));
        });
    }

    [Test]
    public void DeserializeObject_IsCaseInsensitive()
    {
        var result = _ser.Deserialize<PersonRecord>("{\"NAME\":\"Dave\",\"AGE\":25}");
        Assert.That(result.Name, Is.EqualTo("Dave"));
    }

    [Test]
    public void DeserializeObject_EnumFromString()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Deserialize<JsonMergeArrayHandling>("\"Union\"", options);
        Assert.That(result, Is.EqualTo(JsonMergeArrayHandling.Union));
    }

    [Test]
    public void DeserializeObject_DateTime_ParsesFormat()
    {
        var result = _ser.Deserialize<DateTime>("\"2024-06-15T10:30:45.123\"");
        Assert.That(result, Is.EqualTo(new DateTime(2024, 6, 15, 10, 30, 45, 123)));
    }

    [Test]
    public void DeserializeObject_FromStream_ReturnsObject()
    {
        var json = "{\"name\":\"Eve\",\"age\":22}"u8.ToArray();
        using var ms = new MemoryStream(json);
        var result = _ser.Deserialize<PersonRecord>(ms);
        Assert.That(result.Name, Is.EqualTo("Eve"));
    }

    [Test]
    public async Task DeserializeObjectAsync_FromStream_ReturnsObject()
    {
        var json = "{\"name\":\"Frank\",\"age\":33}"u8.ToArray();
        using var ms = new MemoryStream(json);
        var result = await _ser.DeserializeAsync<PersonRecord>(ms);
        Assert.That(result.Name, Is.EqualTo("Frank"));
    }

    // ── String enums ─────────────────────────────────────────────────────────

    [Test]
    public void StringEnum_Serialize_UsesEnumName()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Serialize(JsonMergeNullValueHandling.Ignore, options);
        Assert.That(result, Is.EqualTo("\"Ignore\""));
    }

    [Test]
    public void StringEnum_Deserialize_CaseInsensitive()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        Assert.Multiple(() =>
        {
            Assert.That(_ser.Deserialize<JsonMergeArrayHandling>("\"replace\"", options), Is.EqualTo(JsonMergeArrayHandling.Replace));
            Assert.That(_ser.Deserialize<JsonMergeArrayHandling>("\"CONCAT\"", options), Is.EqualTo(JsonMergeArrayHandling.Concat));
            Assert.That(_ser.Deserialize<JsonMergeArrayHandling>("\"Merge\"", options), Is.EqualTo(JsonMergeArrayHandling.Merge));
        });
    }

    [Test]
    public void StringEnum_RoundTrip()
    {
        var json = _ser.Serialize(new { MergeArrayHandling = JsonMergeArrayHandling.Union });
        var restored = _ser.Deserialize<JsonMergeOptions>(json);
        Assert.That(restored.MergeArrayHandling, Is.EqualTo(JsonMergeArrayHandling.Union));
    }

    // ── Dictionary ───────────────────────────────────────────────────────────

    [Test]
    public void Dictionary_Serialize_StringString()
    {
        var dict = new Dictionary<string, string> { ["key1"] = "val1", ["key2"] = "val2" };
        var result = _ser.Serialize(dict);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("key1").GetString(), Is.EqualTo("val1"));
            Assert.That(doc.RootElement.GetProperty("key2").GetString(), Is.EqualTo("val2"));
        });
    }

    [Test]
    public void Dictionary_Serialize_KeysNotConverted()
    {
        // DictionaryKeyPolicy = null — keys keep original casing
        var dict = new Dictionary<string, int> { ["MyKey"] = 1, ["anotherKey"] = 2 };
        var result = _ser.Serialize(dict);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.TryGetProperty("MyKey", out _), Is.True);
            Assert.That(doc.RootElement.TryGetProperty("anotherKey", out _), Is.True);
        });
    }

    [Test]
    public void Dictionary_Serialize_WithEnumValues()
    {
        var dict = new Dictionary<string, JsonMergeArrayHandling>
        {
            ["first"] = JsonMergeArrayHandling.Concat,
            ["second"] = JsonMergeArrayHandling.Union
        };
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Serialize(dict, options);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("first").GetString(), Is.EqualTo("Concat"));
            Assert.That(doc.RootElement.GetProperty("second").GetString(), Is.EqualTo("Union"));
        });
    }

    [Test]
    public void Dictionary_Deserialize_StringString()
    {
        var result = _ser.Deserialize<Dictionary<string, string>>("{\"a\":\"1\",\"b\":\"2\"}");
        Assert.Multiple(() =>
        {
            Assert.That(result["a"], Is.EqualTo("1"));
            Assert.That(result["b"], Is.EqualTo("2"));
        });
    }

    [Test]
    public void Dictionary_Deserialize_StringObject()
    {
        var result = _ser.Deserialize<Dictionary<string, object>>("{\"num\":42,\"str\":\"hi\"}");
        Assert.Multiple(() =>
        {
            Assert.That(result["num"], Is.EqualTo(42L));
            Assert.That(result["str"], Is.EqualTo("hi"));
        });
    }

    [Test]
    public void Dictionary_RoundTrip()
    {
        var original = new Dictionary<string, string> { ["x"] = "hello", ["y"] = "world" };
        var json = _ser.Serialize(original);
        var restored = _ser.Deserialize<Dictionary<string, string>>(json);
        Assert.That(restored, Is.EqualTo(original));
    }

    [Test]
    public void Dictionary_Clone_IsIndependent()
    {
        var original = new Dictionary<string, string> { ["k"] = "v" };
        var clone = _ser.Clone(original);
        clone["k"] = "changed";
        Assert.That(original["k"], Is.EqualTo("v"));
    }

    // ── Anonymous types ──────────────────────────────────────────────────────

    [Test]
    public void Anonymous_Serialize_KeepsPascalCase()
    {
        var obj = new { FirstName = "Alice", LastName = "Smith", Age = 30 };
        var json = _ser.Serialize(obj);
        Assert.That(json, Is.EqualTo("{\"FirstName\":\"Alice\",\"LastName\":\"Smith\",\"Age\":30}"));
    }

    [Test]
    public void Anonymous_Serialize_NestedAnonymous()
    {
        var obj = new { User = new { Name = "Bob" }, Score = 99 };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("User").GetProperty("Name").GetString(), Is.EqualTo("Bob"));
            Assert.That(doc.RootElement.GetProperty("Score").GetInt32(), Is.EqualTo(99));
        });
    }

    [Test]
    public void Anonymous_Serialize_NullPropertyIncluded()
    {
        var obj = new { Name = "Alice", Tag = (string?)null };
        var json = _ser.Serialize(obj);
        Assert.That(json, Does.Contain("\"Tag\":null"));
    }

    [Test]
    public void Anonymous_Merge_CombinesDistinctProperties()
    {
        var result = _ser.MergeAndSerialize(new List<object>
        {
            new { A = 1, B = 2 },
            new { C = 3 }
        });
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("A").GetInt32(), Is.EqualTo(1));
            Assert.That(doc.RootElement.GetProperty("B").GetInt32(), Is.EqualTo(2));
            Assert.That(doc.RootElement.GetProperty("C").GetInt32(), Is.EqualTo(3));
        });
    }

    [Test]
    public void Anonymous_Merge_SecondOverridesSharedProperty()
    {
        var result = _ser.MergeAndSerialize(new List<object>
        {
            new { X = 1, Y = 2 },
            new { X = 99 }
        });
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("X").GetInt32(), Is.EqualTo(99));
            Assert.That(doc.RootElement.GetProperty("Y").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public void Anonymous_RoundTrip_ViaJsonNode()
    {
        var original = new { Name = "Carol", Count = 7 };
        var json = _ser.Serialize(original);
        var node = _ser.Deserialize<JsonNode>(json);
        Assert.Multiple(() =>
        {
            Assert.That(node!["Name"]!.GetValue<string>(), Is.EqualTo("Carol"));
            Assert.That(node!["Count"]!.GetValue<int>(), Is.EqualTo(7));
        });
    }

    [Test]
    public void Anonymous_SerializeArray_ProducesCorrectJson()
    {
        var obj = new { Tags = new[] { "a", "b", "c" } };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        var tags = doc.RootElement.GetProperty("Tags").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(tags, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    // ── Dynamic / object deserialization ─────────────────────────────────────

    [Test]
    public void Dynamic_DeserializeToObject_ReturnsExpandoObject()
    {
        // ObjectJsonConverter maps JSON objects to ExpandoObject
        var result = _ser.Deserialize<object>("{\"x\":1,\"y\":2}");
        Assert.That(result, Is.InstanceOf<ExpandoObject>());
    }

    [Test]
    public void Dynamic_DeserializeToObject_CanReadProperties()
    {
        var result = (IDictionary<string, object>)_ser.Deserialize<object>("{\"name\":\"dyn\",\"value\":42}");
        Assert.Multiple(() =>
        {
            Assert.That(result["name"], Is.EqualTo("dyn"));
            Assert.That(result["value"], Is.EqualTo(42L));
        });
    }

    [Test]
    public void Dynamic_DeserializeToDynamic_ReturnsExpandoObject()
    {
        dynamic result = _ser.Deserialize<dynamic>("{\"flag\":true}");
        Assert.That(result.flag, Is.True);
        Assert.That(result, Is.InstanceOf<ExpandoObject>());
    }

    [Test]
    public void Dynamic_DeserializeNestedObject_NavigableAsDynamic()
    {
        dynamic result = _ser.Deserialize<object>("{\"outer\":{\"inner\":99}}");
        Assert.That((long)result.outer.inner, Is.EqualTo(99L));
    }

    [Test]
    public void Dynamic_DeserializeArray_ReturnsListOfObject()
    {
        var result = _ser.Deserialize<object>("[1,2,3]");
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<List<object>>());
            Assert.That(((List<object>)result).Cast<long>(), Is.EqualTo(new long[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Dynamic_SerializeJsonNode_ProducesCorrectJson()
    {
        var node = JsonNode.Parse("{\"key\":\"val\"}");
        var json = _ser.Serialize(node);
        Assert.That(json, Is.EqualTo("{\"key\":\"val\"}"));
    }

    [Test]
    public void Dynamic_RoundTrip_ObjectThroughExpando()
    {
        var original = new PersonRecord("RT", 5);
        var json = _ser.Serialize(original);
        var expando = _ser.Deserialize<object>(json);
        var restored = _ser.Deserialize<PersonRecord>(_ser.Serialize(expando));
        Assert.Multiple(() =>
        {
            Assert.That(restored.Name, Is.EqualTo("RT"));
            Assert.That(restored.Age, Is.EqualTo(5));
        });
    }

    // ── MergeAndSerializeObject ──────────────────────────────────────────────

    [Test]
    public void MergeAndSerialize_EmptyList_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize(new List<object>());
        Assert.That(result, Is.EqualTo("{}"));
    }

    [Test]
    public void MergeAndSerialize_NullList_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize((IList<object>?)null);
        Assert.That(result, Is.EqualTo("{}"));
    }

    [Test]
    public void MergeAndSerialize_AllNullItems_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize(new List<object> { null!, null! });
        Assert.That(result, Is.EqualTo("{}"));
    }

    [Test]
    public void MergeAndSerialize_SingleItem_ReturnsSerializedItem()
    {
        var result = _ser.MergeAndSerialize(new List<object> { new { Name = "Alice" } });
        Assert.That(result, Is.EqualTo("{\"Name\":\"Alice\"}"));
    }

    [Test]
    public void MergeAndSerialize_TwoObjects_MergesProperties()
    {
        var result = _ser.MergeAndSerialize([new { A = 1 }, new { B = 2 }]);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("A").GetInt32(), Is.EqualTo(1));
            Assert.That(doc.RootElement.GetProperty("B").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public void MergeAndSerialize_SecondOverridesFirst()
    {
        var result = _ser.MergeAndSerialize([new { Name = "Alice" }, new { Name = "Bob" }]);
        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("Name").GetString(), Is.EqualTo("Bob"));
    }

    [Test]
    public void MergeAndSerialize_ArrayHandling_Replace()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Replace));
        Assert.That(ParseItemsArray(result), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void MergeAndSerialize_ArrayHandling_Concat()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Concat));
        Assert.That(ParseItemsArray(result), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void MergeAndSerialize_ArrayHandling_Union()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 2, 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Union));
        Assert.That(ParseItemsArray(result), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void MergeAndSerialize_ArrayHandling_MergeByIndex()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 9 } }],
            MergeOptsFor(JsonMergeArrayHandling.Merge));
        Assert.That(ParseItemsArray(result), Is.EqualTo(new[] { 9, 2 }));
    }

    [Test]
    public void MergeAndSerialize_NullValueHandling_Ignore_KeepsTargetValue()
    {
        var mergeOpts = new JsonMergeOptions
        {
            BaseOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            },
            MergeArrayHandling = JsonMergeArrayHandling.Replace,
            MergeNullValueHandling = JsonMergeNullValueHandling.Ignore
        };

        mergeOpts.BaseOptions.Converters.Add(new JsonStringEnumConverter());

        var obj1 = new Dictionary<string, object?> { ["name"] = "Alice" };
        var obj2 = new Dictionary<string, object?> { ["name"] = null };

        var result = _ser.MergeAndSerialize([obj1, obj2], mergeOpts);
        using var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("name").GetString(), Is.EqualTo("Alice"));
    }

    // ── PopulateObject ───────────────────────────────────────────────────────

    private class MutablePerson
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    [Test]
    public void PopulateObject_UpdatesExistingProperties()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        _ser.Populate("{\"Name\":\"New\"}", target);
        Assert.Multiple(() =>
        {
            Assert.That(target.Name, Is.EqualTo("New"));
            Assert.That(target.Age, Is.EqualTo(10));
        });
    }

    [Test]
    public void PopulateObject_NullTarget_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _ser.Populate<MutablePerson>("{\"name\":\"X\"}", null!));
    }

    [Test]
    public void PopulateObject_NonObjectJson_DoesNotThrow()
    {
        var target = new MutablePerson { Name = "Keep" };
        Assert.DoesNotThrow(() => _ser.Populate("\"not-an-object\"", target));
        Assert.That(target.Name, Is.EqualTo("Keep"));
    }

    [Test]
    public void PopulateObject_NullPropertyValue_SetsPropertyToNull()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        _ser.Populate("{\"Name\":null}", target);
        Assert.That(target.Name, Is.Null);
    }

    [Test]
    public void PopulateObject_NullPropertyValue_DoesNotThrow()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        Assert.DoesNotThrow(() => _ser.Populate("{\"Name\":null}", target));
    }

    // ── FormatPropertyName ───────────────────────────────────────────────────

    [Test]
    public void FormatPropertyName_ReturnsPropertyNameUnchanged()
    {
        Assert.That(_ser.FormatPropertyName("MyProperty"), Is.EqualTo("MyProperty"));
    }

    [Test]
    public void FormatPropertyName_HandlesDottedPath()
    {
        Assert.That(_ser.FormatPropertyName("ParentObject.ChildProperty"), Is.EqualTo("ParentObject.ChildProperty"));
    }

    [Test]
    public void FormatPropertyName_NullInput_ReturnsNull()
    {
        Assert.That(_ser.FormatPropertyName(null), Is.Null);
    }

    [Test]
    public void FormatPropertyName_EmptyString_ReturnsEmpty()
    {
        Assert.That(_ser.FormatPropertyName(string.Empty), Is.EqualTo(string.Empty));
    }

    // ── Clone / Copy / PatchObject ───────────────────────────────────────────

    [Test]
    public void Clone_ReturnsDeepCopy()
    {
        var original = new MutablePerson { Name = "Alice", Age = 30 };
        var clone = _ser.Clone(original);
        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Name, Is.EqualTo("Alice"));
            Assert.That(clone.Age, Is.EqualTo(30));
        });
    }

    [Test]
    public void Clone_Null_ReturnsDefault()
    {
        var result = _ser.Clone<MutablePerson>(null!);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = new MutablePerson { Name = "Alice" };
        var clone = _ser.Clone(original);
        clone.Name = "Bob";
        Assert.That(original.Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task CloneAsync_ReturnsDeepCopy()
    {
        var original = new MutablePerson { Name = "Async", Age = 5 };
        var clone = await _ser.CloneAsync(original);
        Assert.Multiple(() =>
        {
            Assert.That(clone.Name, Is.EqualTo("Async"));
            Assert.That(clone, Is.Not.SameAs(original));
        });
    }

    [Test]
    public async Task CloneAsync_Null_ReturnsDefault()
    {
        var result = await _ser.CloneAsync<MutablePerson>(null!);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Copy_CopiesPropertiesFromSource()
    {
        var target = new MutablePerson { Name = "Old", Age = 1 };
        var source = new MutablePerson { Name = "New", Age = 99 };
        _ser.Copy(target, source);
        Assert.Multiple(() =>
        {
            Assert.That(target.Name, Is.EqualTo("New"));
            Assert.That(target.Age, Is.EqualTo(99));
        });
    }

    [Test]
    public void Copy_NullSource_ReturnsTargetUnchanged()
    {
        var target = new MutablePerson { Name = "Keep" };
        var result = _ser.Copy(target, null!);
        Assert.That(result!.Name, Is.EqualTo("Keep"));
    }

    [Test]
    public void PatchObject_AppliesPartialJson()
    {
        var obj = new MutablePerson { Name = "Alice", Age = 10 };
        _ser.Patch(obj, "{\"Age\":99}");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Age, Is.EqualTo(99));
            Assert.That(obj.Name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void PatchObject_NullPatch_ReturnsObjectUnchanged()
    {
        var obj = new MutablePerson { Name = "Alice" };
        var result = _ser.Patch(obj, null);
        Assert.That(result.Name, Is.EqualTo("Alice"));
    }

    [Test]
    public void PatchObject_EmptyPatch_ReturnsObjectUnchanged()
    {
        var obj = new MutablePerson { Name = "Alice" };
        var result = _ser.Patch(obj, string.Empty);
        Assert.That(result.Name, Is.EqualTo("Alice"));
    }

    // ── PopulateDefaultOptions ───────────────────────────────────────────────

    [Test]
    public void PopulateDefaultOptions_CopiesConvertersToTarget()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        Assert.That(target.Converters, Is.Not.Empty);
    }

    [Test]
    public void PopulateDefaultOptions_SetsNullNamingPolicyOnTarget()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        Assert.That(target.PropertyNamingPolicy, Is.Null);
    }

    [Test]
    public void PopulateDefaultOptions_DoesNotDuplicateConverters()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        var countFirst = target.Converters.Count;
        _ser.CopyOptions(target);
        Assert.That(target.Converters, Has.Count.EqualTo(countFirst));
    }

    // ── Exception serialization ──────────────────────────────────────────────

    [Test]
    public void Exception_CanConvert_ReturnsTrueForExceptionAndSubclasses()
    {
        var converter = new ExceptionJsonConverter();
        Assert.Multiple(() =>
        {
            Assert.That(converter.CanConvert(typeof(Exception)), Is.True);
            Assert.That(converter.CanConvert(typeof(ArgumentException)), Is.True);
            Assert.That(converter.CanConvert(typeof(InvalidOperationException)), Is.True);
            Assert.That(converter.CanConvert(typeof(string)), Is.False);
            Assert.That(converter.CanConvert(typeof(object)), Is.False);
        });
    }

    [Test]
    public void Exception_Write_ContainsTypeAndMessage()
    {
        var ex = new InvalidOperationException("something went wrong");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("type").GetString(), Is.EqualTo("System.InvalidOperationException"));
            Assert.That(doc.RootElement.GetProperty("message").GetString(), Is.EqualTo("something went wrong"));
        });
    }

    [Test]
    public void Exception_Write_ContainsStackTrace()
    {
        Exception ex;
        try { throw new Exception("boom"); }
        catch (Exception e) { ex = e; }

        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("stackTrace", out var st), Is.True);
        Assert.That(st.GetString(), Does.Contain(nameof(Exception_Write_ContainsStackTrace)));
    }

    [Test]
    public void Exception_Write_NoStackTrace_OmitsProperty()
    {
        var ex = new Exception("no stack");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("stackTrace", out _), Is.False);
    }

    [Test]
    public void Exception_Write_ContainsInnerException()
    {
        var inner = new ArgumentNullException("param");
        var outer = new InvalidOperationException("outer", inner);
        var json = _ser.Serialize(outer);
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("innerException", out var ie), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ie.GetProperty("type").GetString(), Is.EqualTo("System.ArgumentNullException"));
            Assert.That(ie.GetProperty("message").GetString(), Does.Contain("param"));
        });
    }

    [Test]
    public void Exception_Write_NoInnerException_OmitsProperty()
    {
        var ex = new Exception("alone");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("innerException", out _), Is.False);
    }

    [Test]
    public void Exception_Write_AsObjectProperty_Works()
    {
        var obj = new { Error = (object)new ArgumentException("bad arg") };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("Error");
        Assert.That(error.GetProperty("type").GetString(), Is.EqualTo("System.ArgumentException"));
    }

    [Test]
    public void Exception_Write_SubclassUsesFullTypeName()
    {
        var ex = new ArgumentOutOfRangeException("index", "must be positive");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("type").GetString(), Is.EqualTo("System.ArgumentOutOfRangeException"));
    }

    [Test]
    public void Exception_Read_ThrowsNotSupported()
    {
        var converter = new ExceptionJsonConverter();
        Assert.Throws<NotSupportedException>(() =>
        {
            var reader = new Utf8JsonReader("{}"u8);
            converter.Read(ref reader, typeof(Exception), _ser.Options);
        });
    }
}
