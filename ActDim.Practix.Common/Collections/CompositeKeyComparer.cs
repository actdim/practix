using System;
using System.Collections.Generic;

namespace ActDim.Practix.Collections
{
    /// <summary>
    /// Implements <see cref="IEqualityComparer{T}"/> for element-wise equality and hash code calculation over object arrays.
    /// </summary>
    public sealed class CompositeKeyComparer : IEqualityComparer<object[]>
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="CompositeKeyComparer"/>.
        /// </summary>
        public static readonly CompositeKeyComparer Instance = new();

        /// <inheritdoc />
        public int GetHashCode(object[] obj)
        {
            if (obj is null)
            {
                return 0;
            }

            var hc = new HashCode();
            foreach (var item in obj)
            {
                hc.Add(item);
            }

            return hc.ToHashCode();
        }

        /// <inheritdoc />
        public bool Equals(object[] x, object[] y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (!Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
