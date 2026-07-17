using System;
using System.Collections.Generic;

namespace ActDim.Practix.Collections
{
    // MultiKeyComparer
    public sealed class CompositeKeyComparer : IEqualityComparer<object[]>
    {
        public static readonly CompositeKeyComparer Instance = new();

        public int GetHashCode(object[] obj)
        {
            var hc = new HashCode();
            foreach (var item in obj)
                hc.Add(item);
            return hc.ToHashCode();
        }

        public bool Equals(object[] x, object[] y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Length != y.Length) return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!Equals(x[i], y[i]))
                    return false;
            }
            return true;
        }
    }
}
