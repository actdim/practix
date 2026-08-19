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

using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/> and <see cref="IEnumerable"/> sequences.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Splits the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the source sequence.</typeparam>
        /// <param name="source">The sequence to partition.</param>
        /// <param name="size">The maximum size of each chunk.</param>
        /// <returns>A sequence of chunks of size at most <paramref name="size"/>.</returns>
        public static IEnumerable<ReadOnlyCollection<T>> Partition<T>(this IEnumerable<T> source, int size)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.NegativeOrZero(size, nameof(size));

            foreach (var chunk in source.Chunk(size))
            {
                yield return new ReadOnlyCollection<T>(chunk);
            }
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> keeping the first value encountered for duplicate keys (non-greedy).
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionaryNonGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return source.ToDictionaryNonGreedy(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> with a custom key comparer, keeping the first value encountered for duplicate keys (non-greedy).
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionaryNonGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(keySelector, nameof(keySelector));
            Guard.Against.Null(elementSelector, nameof(elementSelector));

            var result = new Dictionary<TKey, TElement>(comparer);

            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!result.ContainsKey(key))
                {
                    result.Add(key, elementSelector(item));
                }
            }

            return result;
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> where subsequent duplicate keys overwrite previous values.
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionaryGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return source.ToDictionaryGreedy(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> with a custom key comparer where subsequent duplicate keys overwrite previous values.
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionaryGreedy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(keySelector, nameof(keySelector));
            Guard.Against.Null(elementSelector, nameof(elementSelector));

            var result = new Dictionary<TKey, TElement>(comparer);

            foreach (var item in source)
            {
                result[keySelector(item)] = elementSelector(item);
            }

            return result;
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> using index-aware key and element selectors.
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TElement> elementSelector)
        {
            return source.ToDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="Dictionary{TKey, TElement}"/> using index-aware key and element selectors and a custom key comparer.
        /// </summary>
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
            return result;
        }

        /// <summary>
        /// Returns true when the sequence is null or contains no elements.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source is null)
            {
                return true;
            }

            if (source is ICollection<T> genericCollection)
            {
                return genericCollection.Count == 0;
            }

            if (source is IReadOnlyCollection<T> readOnlyCollection)
            {
                return readOnlyCollection.Count == 0;
            }

            if (source is ICollection nonGenericCollection)
            {
                return nonGenericCollection.Count == 0;
            }

            return !source.Any();
        }

        /// <summary>
        /// Returns true when the sequence is null or contains no elements.
        /// </summary>
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source is null)
            {
                return true;
            }

            if (source is ICollection collection)
            {
                return collection.Count == 0;
            }

            var enumerator = source.GetEnumerator();
            try
            {
                return !enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the minimum Double value
        /// if the sequence is not empty; otherwise returns the specified default value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The minimum value in the sequence or default value if sequence is empty.</returns>
        public static double MinOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector, double defaultValue)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(selector, nameof(selector));

            var hasValue = false;
            var min = double.MaxValue;

            foreach (var item in source)
            {
                var val = selector(item);
                if (!hasValue || val < min)
                {
                    min = val;
                    hasValue = true;
                }
            }

            return hasValue ? min : defaultValue;
        }

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the maximum Double value
        /// if the sequence is not empty; otherwise returns the specified default value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The maximum value in the sequence or default value if sequence is empty.</returns>
        public static double MaxOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector, double defaultValue)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(selector, nameof(selector));

            var hasValue = false;
            var max = double.MinValue;

            foreach (var item in source)
            {
                var val = selector(item);
                if (!hasValue || val > max)
                {
                    max = val;
                    hasValue = true;
                }
            }

            return hasValue ? max : defaultValue;
        }

        /// <summary>
        /// Returns the 0-based index of the first element in the sequence matching the predicate, or -1 if not found.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.IndexOf((item, index) => predicate(item));
        }

        /// <summary>
        /// Returns the 0-based index of the first element in the sequence matching the index-aware predicate, or -1 if not found.
        /// </summary>
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
            return -1;
        }

        /// <summary>
        /// Evaluates whether the count of items in the sequence matching the predicate reaches or exceeds <paramref name="max"/>.
        /// </summary>
        public static bool EstimateCount<T>(this IEnumerable<T> source, int max, Func<T, bool> predicate)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(predicate, nameof(predicate));

            if (max <= 0)
            {
                return true;
            }

            var count = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    count++;
                    if (count >= max)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether the count of items in the sequence matching the index-aware predicate reaches or exceeds <paramref name="max"/>.
        /// </summary>
        public static bool EstimateCount<T>(this IEnumerable<T> source, int max, Func<T, int, bool> predicate)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(predicate, nameof(predicate));

            if (max <= 0)
            {
                return true;
            }

            var count = 0;
            var index = 0;
            foreach (var item in source)
            {
                if (predicate(item, index))
                {
                    count++;
                    if (count >= max)
                    {
                        return true;
                    }
                }

                index++;
            }

            return false;
        }

        /// <summary>
        /// Determines whether all elements of a sequence satisfy an index-aware condition.
        /// </summary>
        [DebuggerNonUserCode]
        public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(predicate, nameof(predicate));

            var i = 0;

            foreach (var item in source)
            {
                if (!predicate(item, i))
                {
                    return false;
                }

                i++;
            }

            return true;
        }

        /// <summary>
        /// Recursively yields the current elements and all their descendants selected by <paramref name="selector"/>.
        /// </summary>
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

        /// <summary>
        /// Recursively yields the root item and all its descendants selected by <paramref name="selector"/>.
        /// </summary>
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

        /// <summary>
        /// Recursively yields all descendants of the sequence elements selected by <paramref name="selector"/>.
        /// </summary>
        public static IEnumerable<T> Descendants<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector) where T : class
        {
            return source.SelectMany(element => element.Descendants(selector));
        }

        /// <summary>
        /// Recursively yields all descendants of the specified root element selected by <paramref name="selector"/>.
        /// </summary>
        public static IEnumerable<T> Descendants<T>(this T source, Func<T, IEnumerable<T>> selector) where T : class
        {
            return selector(source).SelectMany(element => element.DescendantsAndSelf(selector));
        }

        /// <summary>
        /// Executes a callback for each element while the condition remains true.
        /// </summary>
        [DebuggerNonUserCode]
        public static void While<T>(this IEnumerable<T> source, Func<T, bool> callback)
        {
            source.While((element, index) => callback(element));
        }

        /// <summary>
        /// Executes an index-aware callback for each element while the condition remains true.
        /// </summary>
        [DebuggerNonUserCode]
        public static void While<T>(this IEnumerable<T> source, Func<T, int, bool> callback)
        {
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(callback, nameof(callback));

            var i = 0;
            foreach (var element in source)
            {
                if (!callback(element, i))
                {
                    break;
                }

                i++;
            }
        }

        /// <summary>
        /// Executes a callback for each element until the condition becomes true.
        /// </summary>
        [DebuggerNonUserCode]
        public static void Until<T>(this IEnumerable<T> source, Func<T, bool> callback)
        {
            source.While((element, index) => !callback(element));
        }

        /// <summary>
        /// Executes an index-aware callback for each element until the condition becomes true.
        /// </summary>
        [DebuggerNonUserCode]
        public static void Until<T>(this IEnumerable<T> source, Func<T, int, bool> callback)
        {
            source.While((element, index) => !callback(element, index));
        }
    }
}
