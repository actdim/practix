using System;

namespace ActDim.Practix.Collections
{
    // ObjectArrayKey/MultiKey
    public readonly struct CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object[] _items;
        private readonly int _hashCode;

        public CompositeKey(object[] items)
        {
            _items = items;

            var hc = new HashCode();
            foreach (var item in items)
                hc.Add(item);
            _hashCode = hc.ToHashCode();
        }

        public bool Equals(CompositeKey other)
        {
            // Быстрый путь: если хеши разные — точно не равны,
            // не нужно даже начинать поэлементное сравнение
            if (_hashCode != other._hashCode)
                return false;

            if (_items.Length != other._items.Length)
                return false;

            for (int i = 0; i < _items.Length; i++)
            {
                if (!object.Equals(_items[i], other._items[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) =>
            obj is CompositeKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public static implicit operator CompositeKey(object[] items) => new(items);
    }
}
