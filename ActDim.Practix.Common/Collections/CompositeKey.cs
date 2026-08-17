using System;

namespace ActDim.Practix.Collections
{
    // ObjectArrayKey/MultiKey
    /// <summary>
    /// An immutable composite key built from an ordered array of objects.
    /// Equality and hash code are based on element-wise value comparison using <see cref="object.Equals(object, object)"/>.
    /// </summary>
    public readonly struct CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object[] _items;
        private readonly int _hashCode;

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

        public override bool Equals(object obj) =>
            obj is CompositeKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public static implicit operator CompositeKey(object[] items) => new(items);

        public static bool operator ==(CompositeKey left, CompositeKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompositeKey left, CompositeKey right)
        {
            return !(left == right);
        }
    }
}
