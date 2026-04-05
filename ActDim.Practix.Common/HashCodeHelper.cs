using ActDim.Practix.Extensions;
using System.Collections;

namespace ActDim.Practix
{
    /// <summary>
    /// HashCodeCombiner
    /// </summary>
    public static class HashCodeHelper
    {
        ///// <summary>
        ///// Returns hash code for an array that is generated based on the elements.
        ///// </summary>
        ///// <remarks>
        ///// Hash code returned by this method is guaranteed to be the same for
        ///// arrays with equal elements.
        ///// </remarks>
        ///// <param name="array">
        ///// Array to calculate hash code for.
        ///// </param>
        ///// <returns>
        ///// A hash code for the specified array.
        ///// </returns>        
        public static int CombineHashCode(this IEnumerable collection) //source
        {
            var result = 0;

            if (collection.IsNull())
            {
                return result;
            }

            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;
            int i = 0;

            // const int a = 23;

            foreach (var element in collection)
            {
                var hashCode = 0;
                if (element is IEnumerable elements)
                {
                    hashCode = CombineHashCode(elements);
                }
                else
                {
                    if (!element.IsNull())
                    {
                        hashCode = element.GetHashCode();
                        // EqualityComparer<object>.Default.GetHashCode(element);
                        // RuntimeHelpers.GetHashCode(element);
                    }
                }

                unchecked
                {
                    // result = result * a + hashCode; // prev version

                    // result = (result << 16 | result >> 16) ^ hashCode;
                    // result = ((result << 5) + result) ^ hashCode;

                    // ((uint)hashCode).RotateLeft(16)

                    if (i % 2 == 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                        // hash1 = ((hash1 * 17) + hash1 + (hash1 * 31)) ^ hashCode;
                    }
                    else
                    {
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;
                        // hash2 = ((hash2 * 17) + hash2 + (hash2 * 31)) ^ hashCode;
                    }
                }
                ++i;
            }
            result = hash1 + (hash2 * 1566083941);
            return result;
        }

        //public static int CombineHashCodes(params int[] hashCodes)
        //{
        //    //...
        //}
    }
}