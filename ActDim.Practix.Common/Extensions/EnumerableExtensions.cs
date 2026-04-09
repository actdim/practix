/*
Copyright (c) 2012 Paul Borodaev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#region Usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
#endregion

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    // sealed class PartitionHelper<T> : IEnumerable<IEnumerable<T>>
    // {
    //     readonly IEnumerable<T> _items;
    //     readonly int _partitionSize;
    //     bool _hasMoreItems;
    //     internal PartitionHelper(IEnumerable<T> i, int size)
    //     {
    //         _items = i;
    //         _partitionSize = size;
    //     }
    //     public IEnumerator<IEnumerable<T>> GetEnumerator()
    //     {
    //         using (var enumerator = _items.GetEnumerator())
    //         {
    //             _hasMoreItems = enumerator.MoveNext();
    //             while (_hasMoreItems)
    //             {
    //                 yield return GetNextBatch(enumerator).ToList().AsReadOnly();
    //             }
    //         }
    //     }
    //     IEnumerable<T> GetNextBatch(IEnumerator<T> enumerator)
    //     {
    //         for (var i = 0; i < _partitionSize; ++i)
    //         {
    //             yield return enumerator.Current;
    //             _hasMoreItems = enumerator.MoveNext();
    //             if (!_hasMoreItems)
    //             {
    //                 yield break;
    //             }
    //         }
    //     }
    //     IEnumerator IEnumerable.GetEnumerator()
    //     {
    //         return GetEnumerator();
    //     }
    // }

    /// <summary>
    /// IEnumerable extensions
    /// </summary>
    public static class EnumerableExtensions
    {
        // http://blogs.msdn.com/b/pfxteam/archive/2012/11/16/plinq-and-int32-maxvalue.aspx
        // http://stackoverflow.com/questions/438188/split-a-collection-into-n-parts-with-linq
        // https://code.google.com/p/morelinq/source/browse/MoreLinq/Batch.cs?r=f85495b139a19bce7df2be98ad88754ba8932a28
        // http://stackoverflow.com/questions/13709626/split-an-ienumerablet-into-fixed-sized-chunks-return-an-ienumerableienumerab
        // http://stackoverflow.com/questions/1349491/how-can-i-split-an-ienumerablestring-into-groups-of-ienumerablestring
        // https://www.codeproject.com/Articles/779344/Considerations-on-Efficient-use-of-LINQ-Extension
        // https://github.com/tompazourek/Endless

        // public static ISet<T> ToHashSet<T>(this IEnumerable<T> source)
        // {
        //     return new HashSet<T>(source);
        // }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">sequence</param>
        /// <param name="size">(page/batch)Size</param>
        /// <returns></returns>
        public static IEnumerable<ReadOnlyCollection<T>> Partition<T>(this IEnumerable<T> source, int size) //IEnumerable<IEnumerable<T>>
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.NegativeOrZero(size, nameof(size));

            var count = 0;
            // subset/segment/part/chunk
            T[] portion = null;
            // var portion = new List<T>(size);
            // IList<T> portion = null;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (portion == null)
                    {
                        portion = new T[size];
                        // portion = new List<T>(size);
                    }

                    // portion.Add(enumerator.Current);
                    portion[count] = enumerator.Current;

                    count++;

                    if (count == size)
                    {
                        // yield return portion.AsReadOnly();
                        yield return new ReadOnlyCollection<T>(portion);

                        // portion = new List<T>(size);
                        portion = null;
                        count = 0;
                    }
                }

                // if (count > 0) //portion.Count > 0
                // {
                //     portion.TrimExcess();
                //     yield return portion.AsReadOnly();
                // }

                if (portion != null)
                {
                    Array.Resize(ref portion, count);
                    yield return new ReadOnlyCollection<T>(portion);
                }
            }
        }

        // /// <summary>
        // /// </summary>
        // /// <typeparam name="T"></typeparam>
        // /// <param name="items"></param>
        // /// <param name="size">(page/batch)Size</param>
        // /// <returns></returns>
        // public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items, int size) //Batch/Paginate
        // {
        //     return new PartitionHelper<T>(items, size);
        // }

        // public static IEnumerable<T> AsDuckEnumerable<T>(this object source)
        // {
        // 	dynamic src = source;
        // 	var e = src.GetEnumerator();
        // 	try
        // 	{
        // 		while (e.MoveNext())
        // 		{
        // 		    yield return e.Current;
        // 		}
        // 	}
        // 	finally
        // 	{
        // 		var d = e as IDisposable;
        // 		if (d != null)
        // 		{
        // 			d.Dispose();
        // 		}
        // 	}
        // }

        // public static IEnumerable<TResult> Zip<TFirst, TSecond, TThird, TResult>(
        //     this IEnumerable<TFirst> first,
        //     IEnumerable<TSecond> second,
        //     IEnumerable<TThird> third,
        //     Func<TFirst, TSecond, TThird, TResult> resultSelector)
        // {
        //     // Contract.Requires(first != null && second != null && third != null && resultSelector != null);
        //     using (IEnumerator<TFirst> iterator1 = first.GetEnumerator())
        //     using (IEnumerator<TSecond> iterator2 = second.GetEnumerator())
        //     using (IEnumerator<TThird> iterator3 = third.GetEnumerator())
        //     {
        //         while (iterator1.MoveNext() && iterator2.MoveNext() && iterator3.MoveNext())
        //         {
        //             yield return resultSelector(iterator1.Current, iterator2.Current, iterator3.Current);
        //         }
        //     }
        // }

        // public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> func)
        // {
        //     var ie1 = first.GetEnumerator();
        //     var ie2 = second.GetEnumerator();
        //     while (ie1.MoveNext() && ie2.MoveNext())
        //     {
        //         yield return func(ie1.Current, ie2.Current);
        //     }
        // }

        // public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
        // {
        //     return source.SelectMany(e => Traverse(e, childrenSelector));
        // }

        // public static IEnumerable<T> Traverse<T>(T item, Func<T, IEnumerable<T>> childrenSelector)
        // {
        //     yield return item;
        //     foreach (var subItem in childrenSelector(item).Traverse(childrenSelector))
        //     {
        //         yield return subItem;
        //     }
        // }

        public static Dictionary<TKey, TElement> ToDictionaryNonGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) //Lazy
        {
            return source.ToDictionaryNonGreedy(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        public static Dictionary<TKey, TElement> ToDictionaryNonGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) //Lazy
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(keySelector, nameof(keySelector));
            Guard.Against.Null(elementSelector, nameof(elementSelector));

            var result = new Dictionary<TKey, TElement>(comparer);

            foreach (var item in source) // element
            {
                var key = keySelector(item);
                if (!result.ContainsKey(key))
                {
                    result.Add(key, elementSelector(item));
                }
                // dictionary[keySelector(item)] = elementSelector(item); //Greedy
            }

            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionaryGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) //Lazy
        {
            return source.ToDictionaryGreedy(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        public static Dictionary<TKey, TElement> ToDictionaryGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) //Lazy
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(keySelector, nameof(keySelector));
            Guard.Against.Null(elementSelector, nameof(elementSelector));

            var result = new Dictionary<TKey, TElement>(comparer);

            foreach (var item in source) //element
            {
                result[keySelector(item)] = elementSelector(item);
            }

            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TElement> elementSelector)
        {
            return source.ToDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, TElement>(comparer);
            var i = 0;
            foreach (var item in source)
            {
                var key = keySelector(item, i);
                var element = elementSelector(item, i);
                result.Add(key, element);
                i++;
            }
            return result; ;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any(); // Smart enough (checked source) to discern lazy IEnumerable from ICollection with known Count
        }

        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            return source == null || !source.GetEnumerator().MoveNext();
        }
        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the minimum Double value
        /// if the sequence is not empty; otherwise returns the specified default value.
        /// </summary>
        /// <typeparam name="TSource">The targetType of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The minimum value in the sequence or default value if sequence is empty.</returns>
        public static double MinOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector, double defaultValue)
        {
            if (source.Any())
            {
                return source.Min(selector);
            }

            return defaultValue;
        }

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the maximum Double value
        /// if the sequence is not empty; otherwise returns the specified default value.
        /// </summary>
        /// <typeparam name="TSource">The targetType of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The maximum value in the sequence or default value if sequence is empty.</returns>
        public static double MaxOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector, double defaultValue)
        {
            if (source.Any())
            {
                return source.Max(selector);
            }

            return defaultValue;
        }

        // public static void CopyTo<T>(this IEnumerable<T> source, T[] array, int startIndex)
        // {
        // 	int lowerBound = array.GetLowerBound(0);
        // 	int upperBound = array.GetUpperBound(0);
        // 	if (startIndex < lowerBound)
        // 		throw new ArgumentOutOfRangeException(nameof(startIndex), "The start index must be greater than or equal to the array lower bound");
        // 	if (startIndex > upperBound)
        // 		throw new ArgumentOutOfRangeException(nameof(startIndex), "The start index must be less than or equal to the array upper bound");
        // 	int i = 0;
        // 	foreach (var item in source)
        // 	{
        // 		if (startIndex + i > upperBound)
        // 			throw new ArgumentException("The array capacity is insufficient to copy all items from the source sequence", nameof(startIndex));
        // 		array[startIndex + i] = item;
        // 		//Buffer.BlockCopy(...)?
        // 		i++;
        // 	}
        // }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            var index = -1;
            foreach (var item in source)
            {
                checked
                {
                    ++index;
                }

                if (predicate(item, index))
                {
                    return index;
                }
            }
            return index;
        }

        // LazyCount
        // EstimatedCount
        // filterPredicate/wherePredicate
        public static bool EstimateCount<T>(this IEnumerable<T> source, int max, Func<T, bool> predicate)
        {
            var i = 0;
            var result = false;

            foreach (var item in source)
            {
                if (result = (i >= max))
                {
                    break;
                }

                if (predicate(item))
                {
                    i++;
                }
            }

            return result;
        }

        public static bool EstimateCount<T>(this IEnumerable<T> source, int max, Func<T, int, bool> predicate)
        {
            var i = 0;
            var j = 0;
            var result = false;

            foreach (var item in source) // element
            {
                if (result = (i >= max))
                {
                    break;
                }

                if (predicate(item, j))
                {
                    i++;
                }

                j++;
            }

            return result;
        }

        // TODO: EstimateValue/EstimateAggregation/EstimateComposition/EstimateProduct

        // TODO: Any (Some)

        /// <summary>
        /// Every
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        [DebuggerNonUserCode]
        public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(predicate, nameof(predicate));

            var i = 0;

            foreach (var item in source) // element
            {
                if (!predicate(item, i))
                {
                    return false;
                }

                i++;
            }

            return true;
        }

        // selector - descendBy
        public static IEnumerable<T> DescendantsAndSelf<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector) where T : class
        {
            foreach (var element in source)
            {
                foreach (var descendant in element.DescendantsAndSelf(selector))
                {
                    yield return descendant;
                }
            }
        }

        public static IEnumerable<T> DescendantsAndSelf<T>(this T source, Func<T, IEnumerable<T>> selector) where T : class
        {
            yield return source;
            foreach (var element in selector(source))
            {
                foreach (var descendant in element.DescendantsAndSelf(selector))
                {
                    yield return descendant;
                }
            }
        }

        public static IEnumerable<T> Descendants<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector) where T : class
        {
            return source.SelectMany(element => element.Descendants(selector));
        }

        public static IEnumerable<T> Descendants<T>(this T source, Func<T, IEnumerable<T>> selector) where T : class
        {
            return selector(source).SelectMany(element => element.DescendantsAndSelf(selector));
        }

        // Traverse/DeepMap/Unfold
        // static public IEnumerable<T> Descendants<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        // {
        //     foreach (var element in source)
        //     {
        //         yield return element;
        //         foreach (var descendant in selector(element).Descendants(selector))
        //         {
        //             yield return descendant;
        //         }
        //     }
        // }

        // Each
        /// <summary>
        /// Executes the action for each item in the IEnumerable
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="source">IEnumerable to iterate over</param>
        /// <param name="action">Action to do</param>
        /// <returns>The original list</returns>
        [DebuggerNonUserCode]
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(action, nameof(action));

            foreach (T item in source)
            {
                action(item);
            }

            return source;
        }

        [DebuggerNonUserCode]
        public static void While<T>(this IEnumerable<T> source, Func<T, bool> callback)
        {
            source.While((element, index) => callback(element));
        }

        [DebuggerNonUserCode]
        public static void While<T>(this IEnumerable<T> source, Func<T, int, bool> callback)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(callback, nameof(callback));

            var i = 0;
            foreach (var element in source) //item
            {
                if (!callback(element, i))
                {
                    break;
                }
            }
        }

        [DebuggerNonUserCode]
        public static void Until<T>(this IEnumerable<T> source, Func<T, bool> callback)
        {
            source.While((element, index) => !callback(element));
        }

        [DebuggerNonUserCode]
        public static void Until<T>(this IEnumerable<T> source, Func<T, int, bool> callback)
        {
            source.While((element, index) => !callback(element, index));
        }
    }
}
