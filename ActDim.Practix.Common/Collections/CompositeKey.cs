using System;

namespace ActDim.Practix.Collections;

/// <summary>
/// An immutable composite key built from an ordered array of objects.
/// Equality and hash code are based on element-wise value comparison using <see cref="HashCode"/>
/// and <see cref="object.Equals(object, object)"/>.
/// </summary>
public readonly struct CompositeKey : IEquatable<CompositeKey>
{
    private readonly object[]? _items;
    private readonly int _hashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKey"/> struct using the specified array of key items.
    /// </summary>
    /// <param name="items">The array of objects forming the composite key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    public CompositeKey(object[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items = (object[])items.Clone();

        var hc = new HashCode();
        foreach (var item in _items)
        {
            hc.Add(item);
        }

        _hashCode = hc.ToHashCode();
    }

    /// <summary>
    /// Gets the number of items in the composite key.
    /// </summary>
    public int Length
    {
        get
        {
            return _items?.Length ?? 0;
        }
    }

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is outside the bounds or the key is uninitialized.</exception>
    public object? this[int index]
    {
        get
        {
            if (_items is null)
            {
                throw new IndexOutOfRangeException("The composite key is uninitialized.");
            }

            return _items[index];
        }
    }

    /// <inheritdoc />
    public bool Equals(CompositeKey other)
    {
        if (_hashCode != other._hashCode)
        {
            return false;
        }

        if (ReferenceEquals(_items, other._items))
        {
            return true;
        }

        if (_items is null || other._items is null || _items.Length != other._items.Length)
        {
            return false;
        }

        for (var i = 0; i < _items.Length; i++)
        {
            if (!object.Equals(_items[i], other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CompositeKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _hashCode;
    }

    /// <summary>
    /// Implicitly converts an array of objects to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="items">The array of objects.</param>
    public static implicit operator CompositeKey(object[]? items)
    {
        if (items is null)
        {
            return default;
        }

        return new CompositeKey(items);
    }

    /// <summary>
    /// Compares two <see cref="CompositeKey"/> instances for equality.
    /// </summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>True if the keys are equal; otherwise, false.</returns>
    public static bool operator ==(CompositeKey left, CompositeKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="CompositeKey"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>True if the keys are not equal; otherwise, false.</returns>
    public static bool operator !=(CompositeKey left, CompositeKey right)
    {
        return !(left == right);
    }
}
