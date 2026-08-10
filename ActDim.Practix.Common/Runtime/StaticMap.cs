using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ActDim.Practix.Common.Runtime;

/// <summary>
/// Specifies the lookup implementation used by a static map.
/// </summary>
public enum StaticMapLookup
{
    /// <summary>
    /// Uses <see cref="FrozenDictionary{TKey, TValue}"/>.
    /// </summary>
    Frozen,

    /// <summary>
    /// Generates a specialized lookup delegate during construction.
    /// </summary>
    Generated
}

/// <summary>
/// An immutable map optimized for repeated lookups.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public class StaticMap<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, TValue> _lookup;
    private readonly int _count;

    /// <summary>
    /// Creates a static map.
    /// </summary>
    /// <param name="items">The map entries.</param>
    /// <param name="fallback">
    /// Function invoked when a key is not present.
    /// </param>
    /// <param name="lookup">
    /// Lookup implementation.
    /// </param>
    public StaticMap(
        IEnumerable<KeyValuePair<TKey, TValue>> items,
        Func<TKey, TValue> fallback,
        StaticMapLookup lookup = StaticMapLookup.Frozen)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(fallback);

        var entries = items.ToArray();

        ValidateKeys(entries);

        _count = entries.Length;

        if (entries.Length == 0)
        {
            _lookup = fallback;
            return;
        }

        _lookup = lookup switch
        {
            StaticMapLookup.Frozen =>
                CreateFrozenLookup(entries, fallback),

            StaticMapLookup.Generated =>
                CreateGeneratedLookup(entries, fallback),

            _ =>
                throw new ArgumentOutOfRangeException(nameof(lookup))
        };
    }

    /// <summary>
    /// Gets the number of entries in the map.
    /// </summary>
    public int Count
    {
        get
        {
            return _count;
        }
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// The fallback is invoked when the key is not present.
    /// </summary>
    public TValue this[TKey key]
    {
        get
        {
            return _lookup(key);
        }
    }

    private static void ValidateKeys(
        KeyValuePair<TKey, TValue>[] entries)
    {
        var keys = new HashSet<TKey>();

        foreach (var entry in entries)
        {
            if (!keys.Add(entry.Key))
            {
                throw new ArgumentException(
                    $"An item with the same key has already been added: {entry.Key}",
                    nameof(entries));
            }
        }
    }

    private static Func<TKey, TValue> CreateFrozenLookup(
        KeyValuePair<TKey, TValue>[] entries,
        Func<TKey, TValue> fallback)
    {
        var dictionary = entries.ToFrozenDictionary();

        return key =>
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return fallback(key);
        };
    }

    private static Func<TKey, TValue> CreateGeneratedLookup(
        KeyValuePair<TKey, TValue>[] entries,
        Func<TKey, TValue> fallback)
    {
        if (typeof(TKey) == typeof(string))
        {
            return CreateGeneratedStringLookup(
                entries,
                fallback);
        }

        return CreateGeneratedEqualityLookup(
            entries,
            fallback);
    }

    private static Func<TKey, TValue> CreateGeneratedEqualityLookup(
        KeyValuePair<TKey, TValue>[] entries,
        Func<TKey, TValue> fallback)
    {
        var keyParameter = Expression.Parameter(
            typeof(TKey),
            "key");

        var comparer = EqualityComparer<TKey>.Default;

        Expression body = Expression.Invoke(
            Expression.Constant(fallback),
            keyParameter);

        for (var index = entries.Length - 1;
             index >= 0;
             index--)
        {
            var equals = Expression.Call(
                Expression.Constant(comparer),
                nameof(IEqualityComparer<TKey>.Equals),
                Type.EmptyTypes,
                keyParameter,
                Expression.Constant(
                    entries[index].Key,
                    typeof(TKey)));

            body = Expression.Condition(
                equals,
                Expression.Constant(
                    entries[index].Value,
                    typeof(TValue)),
                body);
        }

        var lambda =
            Expression.Lambda<Func<TKey, TValue>>(
                body,
                keyParameter);

        return lambda.Compile();
    }

    private static Func<TKey, TValue> CreateGeneratedStringLookup(
        KeyValuePair<TKey, TValue>[] entries,
        Func<TKey, TValue> fallback)
    {
        var stringEntries = entries
            .Select(static entry =>
                new StringEntry(
                    (string)(object)entry.Key,
                    entry.Value))
            .ToArray();

        foreach (var entry in stringEntries)
        {
            if (entry.Key is null)
            {
                throw new ArgumentNullException(
                    nameof(entries),
                    "String keys cannot be null.");
            }
        }

        var keyParameter = Expression.Parameter(
            typeof(string),
            "key");

        var fallbackExpression = Expression.Invoke(
            Expression.Constant(fallback),
            Expression.Convert(
                keyParameter,
                typeof(TKey)));

        var body = BuildStringLengthTree(
            keyParameter,
            stringEntries,
            0,
            stringEntries.Length - 1,
            fallbackExpression);

        var lambda =
            Expression.Lambda<Func<string, TValue>>(
                body,
                keyParameter);

        var stringLookup = lambda.Compile();

        return key =>
        {
            if (key is null)
            {
                return fallback(key);
            }

            return stringLookup((string)(object)key);
        };
    }

    private static Expression BuildStringLengthTree(
        ParameterExpression keyParameter,
        StringEntry[] entries,
        int lower,
        int upper,
        Expression fallback)
    {
        if (lower > upper)
        {
            return fallback;
        }

        if (lower == upper)
        {
            return BuildStringCharacterTree(
                keyParameter,
                entries,
                lower,
                upper,
                0,
                fallback);
        }

        var firstLength = entries[lower].Key.Length;
        var lastLength = entries[upper].Key.Length;

        if (firstLength == lastLength)
        {
            return BuildStringCharacterTree(
                keyParameter,
                entries,
                lower,
                upper,
                0,
                fallback);
        }

        var middle = FindLengthBoundary(
            entries,
            lower,
            upper);

        var left = BuildStringLengthTree(
            keyParameter,
            entries,
            lower,
            middle,
            fallback);

        var right = BuildStringLengthTree(
            keyParameter,
            entries,
            middle + 1,
            upper,
            fallback);

        var length = Expression.Property(
            keyParameter,
            nameof(string.Length));

        return Expression.Condition(
            Expression.LessThan(
                length,
                Expression.Constant(
                    entries[middle + 1].Key.Length)),
            left,
            right);
    }

    private static Expression BuildStringCharacterTree(
        ParameterExpression keyParameter,
        StringEntry[] entries,
        int lower,
        int upper,
        int characterIndex,
        Expression fallback)
    {
        if (lower > upper)
        {
            return fallback;
        }

        if (lower == upper)
        {
            return BuildStringEquality(
                keyParameter,
                entries[lower],
                fallback);
        }

        var length = entries[lower].Key.Length;

        if (characterIndex >= length)
        {
            return BuildStringEqualityTree(
                keyParameter,
                entries,
                lower,
                upper,
                fallback);
        }

        var ordered = entries
            .Skip(lower)
            .Take(upper - lower + 1)
            .OrderBy(
                static entry => entry.Key,
                new StringCharacterComparer(characterIndex))
            .ToArray();

        var firstCharacter =
            ordered[0].Key[characterIndex];

        var lastCharacter =
            ordered[^1].Key[characterIndex];

        if (firstCharacter == lastCharacter)
        {
            return BuildStringCharacterTree(
                keyParameter,
                ordered,
                0,
                ordered.Length - 1,
                characterIndex + 1,
                fallback);
        }

        var middle = FindCharacterBoundary(
            ordered,
            characterIndex);

        var left = BuildStringCharacterTree(
            keyParameter,
            ordered,
            0,
            middle,
            characterIndex,
            fallback);

        var right = BuildStringCharacterTree(
            keyParameter,
            ordered,
            middle + 1,
            ordered.Length - 1,
            characterIndex,
            fallback);

        var character = Expression.MakeIndex(
            keyParameter,
            StringCharsProperty,
            new[]
            {
                    Expression.Constant(characterIndex)
            });

        return Expression.Condition(
            Expression.LessThan(
                character,
                Expression.Constant(
                    ordered[middle + 1]
                        .Key[characterIndex])),
            left,
            right);
    }

    private static Expression BuildStringEqualityTree(
        ParameterExpression keyParameter,
        StringEntry[] entries,
        int lower,
        int upper,
        Expression fallback)
    {
        if (lower > upper)
        {
            return fallback;
        }

        var middle = lower + ((upper - lower) / 2);

        var equals = Expression.Call(
            StringEqualsMethod,
            keyParameter,
            Expression.Constant(
                entries[middle].Key));

        var left = BuildStringEqualityTree(
            keyParameter,
            entries,
            lower,
            middle - 1,
            fallback);

        var right = BuildStringEqualityTree(
            keyParameter,
            entries,
            middle + 1,
            upper,
            fallback);

        return Expression.Condition(
            equals,
            Expression.Constant(
                entries[middle].Value,
                typeof(TValue)),
            Expression.Condition(
                Expression.Constant(true),
                right,
                left));
    }

    private static Expression BuildStringEquality(
        ParameterExpression keyParameter,
        StringEntry entry,
        Expression fallback)
    {
        return Expression.Condition(
            Expression.Call(
                StringEqualsMethod,
                keyParameter,
                Expression.Constant(entry.Key)),
            Expression.Constant(
                entry.Value,
                typeof(TValue)),
            fallback);
    }

    private static int FindLengthBoundary(
        StringEntry[] entries,
        int lower,
        int upper)
    {
        var length = entries[lower].Key.Length;

        for (var index = lower + 1;
             index <= upper;
             index++)
        {
            if (entries[index].Key.Length != length)
            {
                return index - 1;
            }
        }

        return upper - 1;
    }

    private static int FindCharacterBoundary(
        StringEntry[] entries,
        int characterIndex)
    {
        var character =
            entries[0].Key[characterIndex];

        for (var index = 1;
             index < entries.Length;
             index++)
        {
            if (entries[index].Key[characterIndex] != character)
            {
                return index - 1;
            }
        }

        return entries.Length - 1;
    }

    private readonly record struct StringEntry(
        string Key,
        TValue Value);

    private sealed class StringCharacterComparer(
        int characterIndex) : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            for (var index = characterIndex;
                 index < x.Length && index < y.Length;
                 index++)
            {
                var result =
                    x[index].CompareTo(y[index]);

                if (result != 0)
                {
                    return result;
                }
            }

            return x.Length.CompareTo(y.Length);
        }
    }

    private static readonly System.Reflection.PropertyInfo
        StringCharsProperty =
            typeof(string).GetProperty("Chars")!;

    private static readonly System.Reflection.MethodInfo
        StringEqualsMethod =
            typeof(string).GetMethod(
                nameof(string.Equals),
                new[]
                {
                        typeof(string),
                        typeof(string)
                })!;
}

/// <summary>
/// An immutable bidirectional map.
/// Both keys and values must be unique.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class StaticBiMap<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly StaticMap<TKey, TValue> _forward;
    private readonly StaticMap<TValue, TKey> _reverse;

    /// <summary>
    /// Creates a bidirectional map.
    /// </summary>
    public StaticBiMap(
        IEnumerable<KeyValuePair<TKey, TValue>> items,
        Func<TKey, TValue> forwardFallback,
        Func<TValue, TKey> reverseFallback,
        StaticMapLookup lookup = StaticMapLookup.Frozen)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(forwardFallback);
        ArgumentNullException.ThrowIfNull(reverseFallback);

        var entries = items.ToArray();

        ValidateEntries(entries);

        _forward = new StaticMap<TKey, TValue>(
            entries,
            forwardFallback,
            lookup);

        var reverseEntries = entries
            .Select(static pair =>
                new KeyValuePair<TValue, TKey>(
                    pair.Value,
                    pair.Key))
            .ToArray();

        _reverse = new StaticMap<TValue, TKey>(
            reverseEntries,
            reverseFallback,
            lookup);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    public TValue this[TKey key]
    {
        get
        {
            return _forward[key];
        }
    }

    /// <summary>
    /// Gets the key associated with the specified value.
    /// </summary>
    public TKey this[TValue value]
    {
        get
        {
            return _reverse[value];
        }
    }

    /// <summary>
    /// Gets the number of entries in the map.
    /// </summary>
    public int Count
    {
        get
        {
            return _forward.Count;
        }
    }

    private static void ValidateEntries(
        KeyValuePair<TKey, TValue>[] entries)
    {
        var keys = new HashSet<TKey>();
        var values = new HashSet<TValue>();

        foreach (var entry in entries)
        {
            if (!keys.Add(entry.Key))
            {
                throw new ArgumentException(
                    $"An item with the same key has already been added: {entry.Key}",
                    nameof(entries));
            }

            if (!values.Add(entry.Value))
            {
                throw new ArgumentException(
                    $"An item with the same value has already been added: {entry.Value}",
                    nameof(entries));
            }
        }
    }
}

/// <summary>
/// Factory methods for static maps.
/// </summary>
public static class StaticMap
{
    /// <summary>
    /// Creates a static map.
    /// </summary>
    public static StaticMap<TKey, TValue> Create<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> items,
        Func<TKey, TValue> fallback,
        StaticMapLookup lookup = StaticMapLookup.Frozen)
        where TKey : notnull
    {
        return new StaticMap<TKey, TValue>(
            items,
            fallback,
            lookup);
    }

    /// <summary>
    /// Creates a static bidirectional map.
    /// </summary>
    public static StaticBiMap<TKey, TValue> CreateBiMap<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> items,
        Func<TKey, TValue> forwardFallback,
        Func<TValue, TKey> reverseFallback,
        StaticMapLookup lookup = StaticMapLookup.Frozen)
        where TKey : notnull
        where TValue : notnull
    {
        return new StaticBiMap<TKey, TValue>(
            items,
            forwardFallback,
            reverseFallback,
            lookup);
    }
}
