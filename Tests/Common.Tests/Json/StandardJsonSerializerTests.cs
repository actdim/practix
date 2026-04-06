using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Common.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Json;

public class StandardJsonSerializerTests
{
    private readonly StandardJsonSerializer _ser;

    public StandardJsonSerializerTests()
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

    [Fact]
    public void GetDefaultOptions_ReturnsNullNamingPolicy()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.Null(options.PropertyNamingPolicy);
    }

    [Fact]
    public void GetDefaultOptions_ReturnsPropertyNameCaseInsensitive()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.True(options.PropertyNameCaseInsensitive);
    }

    [Fact]
    public void GetDefaultOptions_NeverIgnoresProperties()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.Equal(JsonIgnoreCondition.Never, options.DefaultIgnoreCondition);
    }

    [Fact]
    public void GetDefaultOptions_NotIndented()
    {
        var options = _ser.CreateDefaultOptions();
        Assert.False(options.WriteIndented);
    }

    // ── GetDefaultMergeOptions ───────────────────────────────────────────────

    [Fact]
    public void GetDefaultMergeOptions_ReturnsReplaceArrayHandling()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.Equal(JsonMergeArrayHandling.Replace, options.MergeArrayHandling);
    }

    [Fact]
    public void GetDefaultMergeOptions_ReturnsMergeNullValueHandling()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.Equal(JsonMergeNullValueHandling.Merge, options.MergeNullValueHandling);
    }

    [Fact]
    public void GetDefaultMergeOptions_HasBaseOptions()
    {
        var options = _ser.CreateDefaultMergeOptions();
        Assert.NotNull(options.BaseOptions);
    }

    // ── SerializeObject ──────────────────────────────────────────────────────

    [Fact]
    public void SerializeObject_ProducesValidJson()
    {
        var result = _ser.Serialize(new { Name = "Alice" });
        Assert.Equal("{\"Name\":\"Alice\"}", result);
        result = _ser.Serialize("test");
        Assert.Equal("\"test\"", result);
        result = _ser.Serialize(99);
        Assert.Equal("99", result);
    }

    [Fact]
    public void SerializeObject_IncludesNullProperties()
    {
        var result = _ser.Serialize(new { Name = (string?)null, Age = 5 });
        Assert.Multiple(() =>
        {
            Assert.Contains("\"Name\":null", result);
            Assert.Contains("Age", result);
        });
    }

    [Fact]
    public void SerializeObject_Enum_DefaultsToInteger()
    {
        var result = _ser.Serialize(new { Kind = JsonMergeArrayHandling.Concat });
        Assert.Equal("{\"Kind\":0}", result);
    }

    [Fact]
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
            Assert.Contains("\"Concat\"", result);
            Assert.Contains("\"Replace\"", result);
            Assert.Contains("\"Merge\"", result);
            Assert.Contains("\"Union\"", result);
        });
    }

    [Fact]
    public void SerializeObject_DateTimeFormat()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 45, 123, DateTimeKind.Unspecified);
        var result = _ser.Serialize(new { When = dt });
        Assert.Contains("\"2024-06-15T10:30:45.123\"", result);
    }

    [Fact]
    public void SerializeObject_WithExplicitOptions_UsesProvidedOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = _ser.Serialize(new { X = 1 }, options);
        Assert.Contains(Environment.NewLine, result);
    }

    [Fact]
    public void SerializeObject_WithNullOptions_FallsBackToDefault()
    {
        var result = _ser.Serialize(new { Name = "X" }, (JsonSerializerOptions?)null);
        Assert.Equal("{\"Name\":\"X\"}", result);
    }

    // ── SerializeObject (stream) ─────────────────────────────────────────────

    [Fact]
    public void SerializeObject_ToStream_WritesJson()
    {
        using var ms = new MemoryStream();
        _ser.Serialize(new { Value = 42 }, ms);
        var json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Equal("{\"Value\":42}", json);
    }

    [Fact]
    public async Task SerializeObjectAsync_ToStream_WritesJson()
    {
        using var ms = new MemoryStream();
        await _ser.SerializeAsync(new { Value = 99 }, ms);
        var json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Equal("{\"Value\":99}", json);
    }

    // ── DeserializeObject ────────────────────────────────────────────────────

    private record PersonRecord(string Name, int Age);

    [Fact]
    public void DeserializeObject_Generic_ReturnsTypedObject()
    {
        var result = _ser.Deserialize<PersonRecord>("{\"name\":\"Bob\",\"age\":30}");
        Assert.Multiple(() =>
        {
            Assert.NotNull(result);
            Assert.Equal("Bob", result.Name);
            Assert.Equal(30, result.Age);
        });
    }

    [Fact]
    public void DeserializeObject_NonGeneric_ReturnsObject()
    {
        var result = _ser.Deserialize("{\"name\":\"Carol\"}", typeof(PersonRecord));
        Assert.Multiple(() =>
        {
            Assert.IsAssignableFrom<PersonRecord>(result);
            Assert.Equal("Carol", ((PersonRecord)result).Name);
        });
    }

    [Fact]
    public void DeserializeObject_IsCaseInsensitive()
    {
        var result = _ser.Deserialize<PersonRecord>("{\"NAME\":\"Dave\",\"AGE\":25}");
        Assert.Equal("Dave", result.Name);
    }

    [Fact]
    public void DeserializeObject_EnumFromString()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Deserialize<JsonMergeArrayHandling>("\"Union\"", options);
        Assert.Equal(JsonMergeArrayHandling.Union, result);
    }

    [Fact]
    public void DeserializeObject_DateTime_ParsesFormat()
    {
        var result = _ser.Deserialize<DateTime>("\"2024-06-15T10:30:45.123\"");
        Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 45, 123), result);
    }

    [Fact]
    public void DeserializeObject_FromStream_ReturnsObject()
    {
        var json = "{\"name\":\"Eve\",\"age\":22}"u8.ToArray();
        using var ms = new MemoryStream(json);
        var result = _ser.Deserialize<PersonRecord>(ms);
        Assert.Equal("Eve", result.Name);
    }

    [Fact]
    public async Task DeserializeObjectAsync_FromStream_ReturnsObject()
    {
        var json = "{\"name\":\"Frank\",\"age\":33}"u8.ToArray();
        using var ms = new MemoryStream(json);
        var result = await _ser.DeserializeAsync<PersonRecord>(ms);
        Assert.Equal("Frank", result.Name);
    }

    // ── String enums ─────────────────────────────────────────────────────────

    [Fact]
    public void StringEnum_Serialize_UsesEnumName()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        var result = _ser.Serialize(JsonMergeNullValueHandling.Ignore, options);
        Assert.Equal("\"Ignore\"", result);
    }

    [Fact]
    public void StringEnum_Deserialize_CaseInsensitive()
    {
        var options = _ser.CreateDefaultOptions();
        options.Converters.Insert(0, new JsonStringEnumConverter());
        Assert.Multiple(() =>
        {
            Assert.Equal(JsonMergeArrayHandling.Replace, _ser.Deserialize<JsonMergeArrayHandling>("\"replace\"", options));
            Assert.Equal(JsonMergeArrayHandling.Concat, _ser.Deserialize<JsonMergeArrayHandling>("\"CONCAT\"", options));
            Assert.Equal(JsonMergeArrayHandling.Merge, _ser.Deserialize<JsonMergeArrayHandling>("\"Merge\"", options));
        });
    }

    [Fact]
    public void StringEnum_RoundTrip()
    {
        var json = _ser.Serialize(new { MergeArrayHandling = JsonMergeArrayHandling.Union });
        var restored = _ser.Deserialize<JsonMergeOptions>(json);
        Assert.Equal(JsonMergeArrayHandling.Union, restored.MergeArrayHandling);
    }

    // ── Dictionary ───────────────────────────────────────────────────────────

    [Fact]
    public void Dictionary_Serialize_StringString()
    {
        var dict = new Dictionary<string, string> { ["key1"] = "val1", ["key2"] = "val2" };
        var result = _ser.Serialize(dict);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.Equal("val1", doc.RootElement.GetProperty("key1").GetString());
            Assert.Equal("val2", doc.RootElement.GetProperty("key2").GetString());
        });
    }

    [Fact]
    public void Dictionary_Serialize_KeysNotConverted()
    {
        var dict = new Dictionary<string, int> { ["MyKey"] = 1, ["anotherKey"] = 2 };
        var result = _ser.Serialize(dict);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.True(doc.RootElement.TryGetProperty("MyKey", out _));
            Assert.True(doc.RootElement.TryGetProperty("anotherKey", out _));
        });
    }

    [Fact]
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
            Assert.Equal("Concat", doc.RootElement.GetProperty("first").GetString());
            Assert.Equal("Union", doc.RootElement.GetProperty("second").GetString());
        });
    }

    [Fact]
    public void Dictionary_Deserialize_StringString()
    {
        var result = _ser.Deserialize<Dictionary<string, string>>("{\"a\":\"1\",\"b\":\"2\"}");
        Assert.Multiple(() =>
        {
            Assert.Equal("1", result["a"]);
            Assert.Equal("2", result["b"]);
        });
    }

    [Fact]
    public void Dictionary_Deserialize_StringObject()
    {
        var result = _ser.Deserialize<Dictionary<string, object>>("{\"num\":42,\"str\":\"hi\"}");
        Assert.Multiple(() =>
        {
            Assert.Equal(42L, result["num"]);
            Assert.Equal("hi", result["str"]);
        });
    }

    [Fact]
    public void Dictionary_RoundTrip()
    {
        var original = new Dictionary<string, string> { ["x"] = "hello", ["y"] = "world" };
        var json = _ser.Serialize(original);
        var restored = _ser.Deserialize<Dictionary<string, string>>(json);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void Dictionary_Clone_IsIndependent()
    {
        var original = new Dictionary<string, string> { ["k"] = "v" };
        var clone = _ser.Clone(original);
        clone["k"] = "changed";
        Assert.Equal("v", original["k"]);
    }

    // ── Anonymous types ──────────────────────────────────────────────────────

    [Fact]
    public void Anonymous_Serialize_KeepsPascalCase()
    {
        var obj = new { FirstName = "Alice", LastName = "Smith", Age = 30 };
        var json = _ser.Serialize(obj);
        Assert.Equal("{\"FirstName\":\"Alice\",\"LastName\":\"Smith\",\"Age\":30}", json);
    }

    [Fact]
    public void Anonymous_Serialize_NestedAnonymous()
    {
        var obj = new { User = new { Name = "Bob" }, Score = 99 };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.Equal("Bob", doc.RootElement.GetProperty("User").GetProperty("Name").GetString());
            Assert.Equal(99, doc.RootElement.GetProperty("Score").GetInt32());
        });
    }

    [Fact]
    public void Anonymous_Serialize_NullPropertyIncluded()
    {
        var obj = new { Name = "Alice", Tag = (string)null };
        var json = _ser.Serialize(obj);
        Assert.Contains("\"Tag\":null", json);
    }

    [Fact]
    public void Anonymous_Merge_CombinesDistinctProperties()
    {
        var result = _ser.MergeAndSerialize(
        [
            new { A = 1, B = 2 },
            new { C = 3 }
        ]);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(1, doc.RootElement.GetProperty("A").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("B").GetInt32());
            Assert.Equal(3, doc.RootElement.GetProperty("C").GetInt32());
        });
    }

    [Fact]
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
            Assert.Equal(99, doc.RootElement.GetProperty("X").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("Y").GetInt32());
        });
    }

    [Fact]
    public void Anonymous_RoundTrip_ViaJsonNode()
    {
        var original = new { Name = "Carol", Count = 7 };
        var json = _ser.Serialize(original);
        var node = _ser.Deserialize<JsonNode>(json);
        Assert.Multiple(() =>
        {
            Assert.Equal("Carol", node!["Name"]!.GetValue<string>());
            Assert.Equal(7, node!["Count"]!.GetValue<int>());
        });
    }

    [Fact]
    public void Anonymous_SerializeArray_ProducesCorrectJson()
    {
        var obj = new { Tags = new[] { "a", "b", "c" } };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        var tags = doc.RootElement.GetProperty("Tags").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(new[] { "a", "b", "c" }, tags);
    }

    // ── Dynamic / object deserialization ─────────────────────────────────────

    [Fact]
    public void Dynamic_DeserializeToObject_ReturnsExpandoObject()
    {
        var result = _ser.Deserialize<object>("{\"x\":1,\"y\":2}");
        Assert.IsAssignableFrom<ExpandoObject>(result);
    }

    [Fact]
    public void Dynamic_DeserializeToObject_CanReadProperties()
    {
        var result = (IDictionary<string, object>)_ser.Deserialize<object>("{\"name\":\"dyn\",\"value\":42}");
        Assert.Multiple(() =>
        {
            Assert.Equal("dyn", result["name"]);
            Assert.Equal(42L, result["value"]);
        });
    }

    [Fact]
    public void Dynamic_DeserializeToDynamic_ReturnsExpandoObject()
    {
        dynamic result = _ser.Deserialize<dynamic>("{\"flag\":true}");
        Assert.True(result.flag);
        Assert.IsAssignableFrom<ExpandoObject>(result);
    }

    [Fact]
    public void Dynamic_DeserializeNestedObject_NavigableAsDynamic()
    {
        dynamic result = _ser.Deserialize<object>("{\"outer\":{\"inner\":99}}");
        Assert.Equal(99L, (long)result.outer.inner);
    }

    [Fact]
    public void Dynamic_DeserializeArray_ReturnsListOfObject()
    {
        var result = _ser.Deserialize<object>("[1,2,3]");
        Assert.Multiple(() =>
        {
            Assert.IsAssignableFrom<List<object>>(result);
            Assert.Equal(new long[] { 1, 2, 3 }, ((List<object>)result).Cast<long>());
        });
    }

    [Fact]
    public void Dynamic_SerializeJsonNode_ProducesCorrectJson()
    {
        var node = JsonNode.Parse("{\"key\":\"val\"}");
        var json = _ser.Serialize(node);
        Assert.Equal("{\"key\":\"val\"}", json);
    }

    [Fact]
    public void Dynamic_RoundTrip_ObjectThroughExpando()
    {
        var original = new PersonRecord("RT", 5);
        var json = _ser.Serialize(original);
        var expando = _ser.Deserialize<object>(json);
        var restored = _ser.Deserialize<PersonRecord>(_ser.Serialize(expando));
        Assert.Multiple(() =>
        {
            Assert.Equal("RT", restored.Name);
            Assert.Equal(5, restored.Age);
        });
    }

    // ── MergeAndSerializeObject ──────────────────────────────────────────────

    [Fact]
    public void MergeAndSerialize_EmptyList_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize(new List<object>());
        Assert.Equal("{}", result);
    }

    [Fact]
    public void MergeAndSerialize_NullList_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize((IList<object>)null);
        Assert.Equal("{}", result);
    }

    [Fact]
    public void MergeAndSerialize_AllNullItems_ReturnsEmptyObject()
    {
        var result = _ser.MergeAndSerialize(new List<object> { null!, null! });
        Assert.Equal("{}", result);
    }

    [Fact]
    public void MergeAndSerialize_SingleItem_ReturnsSerializedItem()
    {
        var result = _ser.MergeAndSerialize(new List<object> { new { Name = "Alice" } });
        Assert.Equal("{\"Name\":\"Alice\"}", result);
    }

    [Fact]
    public void MergeAndSerialize_TwoObjects_MergesProperties()
    {
        var result = _ser.MergeAndSerialize([new { A = 1 }, new { B = 2 }]);
        using var doc = JsonDocument.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(1, doc.RootElement.GetProperty("A").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("B").GetInt32());
        });
    }

    [Fact]
    public void MergeAndSerialize_SecondOverridesFirst()
    {
        var result = _ser.MergeAndSerialize([new { Name = "Alice" }, new { Name = "Bob" }]);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("Bob", doc.RootElement.GetProperty("Name").GetString());
    }

    [Fact]
    public void MergeAndSerialize_ArrayHandling_Replace()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Replace));
        Assert.Equal(new[] { 3 }, ParseItemsArray(result));
    }

    [Fact]
    public void MergeAndSerialize_ArrayHandling_Concat()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Concat));
        Assert.Equal(new[] { 1, 2, 3 }, ParseItemsArray(result));
    }

    [Fact]
    public void MergeAndSerialize_ArrayHandling_Union()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 2, 3 } }],
            MergeOptsFor(JsonMergeArrayHandling.Union));
        Assert.Equal(new[] { 1, 2, 3 }, ParseItemsArray(result));
    }

    [Fact]
    public void MergeAndSerialize_ArrayHandling_MergeByIndex()
    {
        var result = _ser.MergeAndSerialize(
            [new { Items = new[] { 1, 2 } }, new { Items = new[] { 9 } }],
            MergeOptsFor(JsonMergeArrayHandling.Merge));
        Assert.Equal(new[] { 9, 2 }, ParseItemsArray(result));
    }

    [Fact]
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

        var obj1 = new Dictionary<string, object> { ["name"] = "Alice" };
        var obj2 = new Dictionary<string, object> { ["name"] = null };
        var result = _ser.MergeAndSerialize([obj1, obj2], mergeOpts);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
    }

    // ── PopulateObject ───────────────────────────────────────────────────────

    private class MutablePerson
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public void PopulateObject_UpdatesExistingProperties()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        _ser.Populate("{\"Name\":\"New\"}", target);
        Assert.Multiple(() =>
        {
            Assert.Equal("New", target.Name);
            Assert.Equal(10, target.Age);
        });
    }

    [Fact]
    public void PopulateObject_NullTarget_DoesNotThrow()
    {
        var ex = Record.Exception(() => _ser.Populate<MutablePerson>("{\"name\":\"X\"}", null!));
        Assert.Null(ex);
    }

    [Fact]
    public void PopulateObject_NonObjectJson_DoesNotThrow()
    {
        var target = new MutablePerson { Name = "Keep" };
        var ex = Record.Exception(() => _ser.Populate("\"not-an-object\"", target));
        Assert.Null(ex);
        Assert.Equal("Keep", target.Name);
    }

    [Fact]
    public void PopulateObject_NullPropertyValue_SetsPropertyToNull()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        _ser.Populate("{\"Name\":null}", target);
        Assert.Null(target.Name);
    }

    [Fact]
    public void PopulateObject_NullPropertyValue_DoesNotThrow()
    {
        var target = new MutablePerson { Name = "Old", Age = 10 };
        var ex = Record.Exception(() => _ser.Populate("{\"Name\":null}", target));
        Assert.Null(ex);
    }

    // ── FormatPropertyName ───────────────────────────────────────────────────

    [Fact]
    public void FormatPropertyName_ReturnsPropertyNameUnchanged()
    {
        Assert.Equal("MyProperty", _ser.FormatPropertyName("MyProperty"));
    }

    [Fact]
    public void FormatPropertyName_HandlesDottedPath()
    {
        Assert.Equal("ParentObject.ChildProperty", _ser.FormatPropertyName("ParentObject.ChildProperty"));
    }

    [Fact]
    public void FormatPropertyName_NullInput_ReturnsNull()
    {
        Assert.Null(_ser.FormatPropertyName(null));
    }

    [Fact]
    public void FormatPropertyName_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _ser.FormatPropertyName(string.Empty));
    }

    // ── Clone / Copy / PatchObject ───────────────────────────────────────────

    [Fact]
    public void Clone_ReturnsDeepCopy()
    {
        var original = new MutablePerson { Name = "Alice", Age = 30 };
        var clone = _ser.Clone(original);
        Assert.Multiple(() =>
        {
            Assert.NotSame(original, clone);
            Assert.Equal("Alice", clone.Name);
            Assert.Equal(30, clone.Age);
        });
    }

    [Fact]
    public void Clone_Null_ReturnsDefault()
    {
        var result = _ser.Clone<MutablePerson>(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = new MutablePerson { Name = "Alice" };
        var clone = _ser.Clone(original);
        clone.Name = "Bob";
        Assert.Equal("Alice", original.Name);
    }

    [Fact]
    public async Task CloneAsync_ReturnsDeepCopy()
    {
        var original = new MutablePerson { Name = "Async", Age = 5 };
        var clone = await _ser.CloneAsync(original);
        Assert.Multiple(() =>
        {
            Assert.Equal("Async", clone.Name);
            Assert.NotSame(original, clone);
        });
    }

    [Fact]
    public async Task CloneAsync_Null_ReturnsDefault()
    {
        var result = await _ser.CloneAsync<MutablePerson>(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Copy_CopiesPropertiesFromSource()
    {
        var target = new MutablePerson { Name = "Old", Age = 1 };
        var source = new MutablePerson { Name = "New", Age = 99 };
        _ser.Copy(target, source);
        Assert.Multiple(() =>
        {
            Assert.Equal("New", target.Name);
            Assert.Equal(99, target.Age);
        });
    }

    [Fact]
    public void Copy_NullSource_ReturnsTargetUnchanged()
    {
        var target = new MutablePerson { Name = "Keep" };
        var result = _ser.Copy(target, null!);
        Assert.Equal("Keep", result!.Name);
    }

    [Fact]
    public void PatchObject_AppliesPartialJson()
    {
        var obj = new MutablePerson { Name = "Alice", Age = 10 };
        _ser.Patch(obj, "{\"Age\":99}");
        Assert.Multiple(() =>
        {
            Assert.Equal(99, obj.Age);
            Assert.Equal("Alice", obj.Name);
        });
    }

    [Fact]
    public void PatchObject_NullPatch_ReturnsObjectUnchanged()
    {
        var obj = new MutablePerson { Name = "Alice" };
        var result = _ser.Patch(obj, null);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public void PatchObject_EmptyPatch_ReturnsObjectUnchanged()
    {
        var obj = new MutablePerson { Name = "Alice" };
        var result = _ser.Patch(obj, string.Empty);
        Assert.Equal("Alice", result.Name);
    }

    // ── PopulateDefaultOptions ───────────────────────────────────────────────

    [Fact]
    public void PopulateDefaultOptions_CopiesConvertersToTarget()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        Assert.NotEmpty(target.Converters);
    }

    [Fact]
    public void PopulateDefaultOptions_SetsNullNamingPolicyOnTarget()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        Assert.Null(target.PropertyNamingPolicy);
    }

    [Fact]
    public void PopulateDefaultOptions_DoesNotDuplicateConverters()
    {
        var target = new JsonSerializerOptions();
        _ser.CopyOptions(target);
        var countFirst = target.Converters.Count;
        _ser.CopyOptions(target);
        Assert.Equal(countFirst, target.Converters.Count);
    }

    // ── Exception serialization ──────────────────────────────────────────────

    [Fact]
    public void Exception_CanConvert_ReturnsTrueForExceptionAndSubclasses()
    {
        var converter = new ExceptionJsonConverter();
        Assert.Multiple(() =>
        {
            Assert.True(converter.CanConvert(typeof(Exception)));
            Assert.True(converter.CanConvert(typeof(ArgumentException)));
            Assert.True(converter.CanConvert(typeof(InvalidOperationException)));
            Assert.False(converter.CanConvert(typeof(string)));
            Assert.False(converter.CanConvert(typeof(object)));
        });
    }

    [Fact]
    public void Exception_Write_ContainsTypeAndMessage()
    {
        var ex = new InvalidOperationException("something went wrong");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.Equal("System.InvalidOperationException", doc.RootElement.GetProperty("type").GetString());
            Assert.Equal("something went wrong", doc.RootElement.GetProperty("message").GetString());
        });
    }

    [Fact]
    public void Exception_Write_ContainsStackTrace()
    {
        Exception ex;
        try { throw new Exception("boom"); }
        catch (Exception e) { ex = e; }

        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("stackTrace", out var st));
        Assert.Contains(nameof(Exception_Write_ContainsStackTrace), st.GetString());
    }

    [Fact]
    public void Exception_Write_NoStackTrace_OmitsProperty()
    {
        var ex = new Exception("no stack");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public void Exception_Write_ContainsInnerException()
    {
        var inner = new ArgumentNullException("param");
        var outer = new InvalidOperationException("outer", inner);
        var json = _ser.Serialize(outer);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("innerException", out var ie));
        Assert.Multiple(() =>
        {
            Assert.Equal("System.ArgumentNullException", ie.GetProperty("type").GetString());
            Assert.Contains("param", ie.GetProperty("message").GetString());
        });
    }

    [Fact]
    public void Exception_Write_NoInnerException_OmitsProperty()
    {
        var ex = new Exception("alone");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("innerException", out _));
    }

    [Fact]
    public void Exception_Write_AsObjectProperty_Works()
    {
        var obj = new { Error = (object)new ArgumentException("bad arg") };
        var json = _ser.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("Error");
        Assert.Equal("System.ArgumentException", error.GetProperty("type").GetString());
    }

    [Fact]
    public void Exception_Write_SubclassUsesFullTypeName()
    {
        var ex = new ArgumentOutOfRangeException("index", "must be positive");
        var json = _ser.Serialize(ex);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("System.ArgumentOutOfRangeException", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
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
