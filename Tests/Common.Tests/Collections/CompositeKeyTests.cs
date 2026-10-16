using ActDim.Practix.Collections;
using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Practix.Common.Tests.Collections;

public class CompositeKeyTests
{
    [Fact]
    public void Constructor_NullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeKey(null!));
    }

    [Fact]
    public void Constructor_EmptyArray_CreatesValidKey()
    {
        var key1 = new CompositeKey(Array.Empty<object>());
        var key2 = new CompositeKey(Array.Empty<object>());

        Assert.Equal(0, key1.Length);
        Assert.True(key1.Equals(key2));
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void DefaultStruct_EqualsAndOperators_DoNotThrow()
    {
        var defaultKey1 = default(CompositeKey);
        var defaultKey2 = default(CompositeKey);
        var initializedKey = new CompositeKey(new object[] { "test" });

        Assert.Equal(0, defaultKey1.Length);
        Assert.True(defaultKey1.Equals(defaultKey2));
        Assert.True(defaultKey1 == defaultKey2);
        Assert.False(defaultKey1 != defaultKey2);
        Assert.Equal(0, defaultKey1.GetHashCode());

        Assert.False(defaultKey1.Equals(initializedKey));
        Assert.False(initializedKey.Equals(defaultKey1));
        Assert.False(defaultKey1 == initializedKey);
        Assert.True(defaultKey1 != initializedKey);

        Assert.Throws<IndexOutOfRangeException>(() => defaultKey1[0]);
    }

    [Fact]
    public void StructuralEquality_IdenticalElements_AreEqualAndShareHashCode()
    {
        var key1 = new CompositeKey(new object[] { "tenant_1", 42, true });
        var key2 = new CompositeKey(new object[] { "tenant_1", 42, true });

        Assert.True(key1.Equals(key2));
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.True(key1.Equals((object)key2));
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void StructuralEquality_DifferentElementsOrLengths_AreNotEqual()
    {
        var key1 = new CompositeKey(new object[] { "tenant_1", 42 });
        var key2 = new CompositeKey(new object[] { "tenant_1", 43 });
        var key3 = new CompositeKey(new object[] { "tenant_1", 42, "extra" });

        Assert.False(key1.Equals(key2));
        Assert.True(key1 != key2);
        Assert.False(key1.Equals(key3));
        Assert.False(key1.Equals(null));
        Assert.False(key1.Equals("not-a-composite-key"));
    }

    [Fact]
    public void StructuralEquality_NullElements_HandledCorrectly()
    {
        var key1 = new CompositeKey(new object[] { "a", null!, "b" });
        var key2 = new CompositeKey(new object[] { "a", null!, "b" });
        var key3 = new CompositeKey(new object[] { "a", "not-null", "b" });

        Assert.True(key1.Equals(key2));
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
        Assert.False(key1.Equals(key3));
    }

    [Fact]
    public void ArrayMutation_DoesNotMutateInternalState()
    {
        var array = new object[] { "initial", 1 };
        var key = new CompositeKey(array);

        array[0] = "mutated";
        array[1] = 999;

        Assert.Equal("initial", key[0]);
        Assert.Equal(1, key[1]);

        var matchKey = new CompositeKey(new object[] { "initial", 1 });
        Assert.True(key.Equals(matchKey));
    }

    [Fact]
    public void IndexerAndLength_AccessElementsCorrectly()
    {
        var key = new CompositeKey(new object[] { "first", 2, 3.5 });

        Assert.Equal(3, key.Length);
        Assert.Equal("first", key[0]);
        Assert.Equal(2, key[1]);
        Assert.Equal(3.5, key[2]);

        Assert.Throws<IndexOutOfRangeException>(() => key[3]);
        Assert.Throws<IndexOutOfRangeException>(() => key[-1]);
    }

    [Fact]
    public void ImplicitConversion_CreatesIdenticalKey()
    {
        object[] items = { "k", 10 };
        CompositeKey key = items;

        Assert.Equal(2, key.Length);
        Assert.Equal("k", key[0]);
        Assert.Equal(10, key[1]);
    }

    [Fact]
    public void DictionaryAndHashSet_LookupSucceeds()
    {
        var dict = new Dictionary<CompositeKey, string>();
        var set = new HashSet<CompositeKey>();

        var key1 = new CompositeKey(new object[] { "a", 1 });
        var key2 = new CompositeKey(new object[] { "b", 2 });

        dict[key1] = "Value1";
        dict[key2] = "Value2";

        set.Add(key1);
        set.Add(key2);

        var lookup1 = new CompositeKey(new object[] { "a", 1 });
        var lookup2 = new CompositeKey(new object[] { "b", 2 });
        var missing = new CompositeKey(new object[] { "c", 3 });

        Assert.True(dict.TryGetValue(lookup1, out var val1));
        Assert.Equal("Value1", val1);

        Assert.True(dict.TryGetValue(lookup2, out var val2));
        Assert.Equal("Value2", val2);

        Assert.False(dict.TryGetValue(missing, out _));

        Assert.Contains(lookup1, set);
        Assert.Contains(lookup2, set);
        Assert.DoesNotContain(missing, set);
    }
}
