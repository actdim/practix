using System;

namespace ActDim.Practix.Collections
{
    /// <summary>
    /// An immutable composite key built from an ordered array of objects.
    /// Equality and hash code are based on element-wise value comparison using <see cref="object.Equals(object, object)"/>.
    /// </summary>
    public readonly struct CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object[] _items;
        private readonly int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeKey"/> struct using the specified array of key items.
        /// </summary>
        /// <param name="items">The array of objects forming the composite key.</param>
        public CompositeKey(object[] items)
        {
            _items = items;

            var hc = new HashCode();
            foreach (var item in items)
            {
                hc.Add(item);
            }

            _hashCode = hc.ToHashCode();
        }

        /// <inheritdoc />
        public bool Equals(CompositeKey other)
        {
            if (_hashCode != other._hashCode)
            {
                return false;
            }

            if (_items.Length != other._items.Length)
            {
                return false;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                if (!object.Equals(_items[i], other._items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
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
        public static implicit operator CompositeKey(object[] items) => new(items);

        /// <summary>
        /// Compares two <see cref="CompositeKey"/> instances for equality.
        /// </summary>
        public static bool operator ==(CompositeKey left, CompositeKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="CompositeKey"/> instances for inequality.
        /// </summary>
        public static bool operator !=(CompositeKey left, CompositeKey right)
        {
            return !(left == right);
        }
    }
}
