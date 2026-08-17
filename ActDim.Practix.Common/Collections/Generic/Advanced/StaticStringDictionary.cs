using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Practix.Collections.Generic.Specialized
{
    /// <summary>
    /// Factory for creating compiled expression tree switch dictionaries over static string keys.
    /// </summary>
    public static class StaticStringDictionary
    {
        /// <summary>
        /// Creates a <see cref="StaticStringDictionary{TVal}"/> compiled switch mapping over the provided key-value dictionary.
        /// </summary>
        /// <typeparam name="TVal">The value type.</typeparam>
        /// <param name="dict">The source key-value dictionary.</param>
        /// <param name="fallback">Fallback delegate called on missing keys.</param>
        /// <returns>A new <see cref="StaticStringDictionary{TVal}"/> instance.</returns>
        public static StaticStringDictionary<TVal> Create<TVal>(IEnumerable<KeyValuePair<string, TVal>> dict, Func<string, TVal> fallback)
        {
            return new StaticStringDictionary<TVal>(dict, fallback);
        }
    }

    /// <summary>
    /// A high-performance read-only dictionary compiled as expression tree char-matching switches over static string keys.
    /// </summary>
    /// <typeparam name="TVal">The value type.</typeparam>
    public class StaticStringDictionary<TVal> : IDictionary<string, TVal>
    {
        private readonly Func<string, TVal> _fallback;
        private readonly Func<string, TVal> _switchFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticStringDictionary{TVal}"/> class.
        /// </summary>
        /// <param name="dict">The key-value pairs.</param>
        /// <param name="fallback">Fallback delegate for missing keys.</param>
        public StaticStringDictionary(IEnumerable<KeyValuePair<string, TVal>> dict, Func<string, TVal> fallback)
        {
            _fallback = fallback;
            _switchFunction = CreateSwitch(dict);
        }

        private struct SwitchCase
        {
            public readonly string Key;
            public readonly TVal Value;

            public SwitchCase(string key, TVal value)
            {
                Key = key;
                Value = value;
            }

            public override string ToString()
            {
                return Key + " " + Value.ToString();
            }
        }

        private Func<string, TVal> CreateSwitch(IEnumerable<KeyValuePair<string, TVal>> dict)
        {
            var cases = dict.Select(pair => new SwitchCase(pair.Key, pair.Value)).ToList();
            ParameterExpression keyParameter = Expression.Parameter(typeof(string), "key");
            var expr = Expression.Lambda<Func<string, TVal>>(
                SwitchOnLength(keyParameter, cases.OrderBy(switchCase => switchCase.Key.Length).ToArray(), 0, cases.Count - 1),
                new ParameterExpression[] { keyParameter }
            );
            var del = expr.Compile();
            return del;
        }

        private Expression SwitchOnLength(ParameterExpression keyParameter, SwitchCase[] switchCases, int lower, int upper)
        {
            if (switchCases[lower].Key.Length == switchCases[upper].Key.Length)
            {
                return SwitchOnChar(keyParameter, switchCases.Skip(lower).Take(upper - lower + 1).ToArray(), 0, 0, upper - lower);
            }

            int middle = GetIndexOfFirstDifferentCaseFromUp(switchCases, lower, MidPoint(lower, upper), upper, switchCase => switchCase.Key.Length);
            if (middle == -1)
            {
                throw new InvalidOperationException();
            }

            return Expression.Condition(
                Expression.LessThan(Expression.Call(keyParameter, StringLength), Expression.Constant(switchCases[middle + 1].Key.Length)),
                SwitchOnLength(keyParameter, switchCases, lower, middle),
                SwitchOnLength(keyParameter, switchCases, middle + 1, upper));
        }

        private Expression SwitchOnChar(ParameterExpression keyParameter, SwitchCase[] switchCases, int index, int lower, int upper)
        {
            if (index == switchCases[upper].Key.Length)
            {
                return null;
            }

            if (lower == upper)
            {
                return Expression.Condition(
                    Expression.Call(StringEquals, keyParameter, Expression.Constant(switchCases[lower].Key)),
                    Expression.Convert(Expression.Constant(switchCases[lower].Value), typeof(TVal)),
                    Expression.Invoke(Expression.Constant(_fallback), keyParameter));
            }

            switchCases = switchCases.Skip(lower).Take(upper - lower + 1)
                .OrderBy(switchCase => switchCase.Key, StaticStringDictionaryComparer.For(index)).ToArray();

            upper = upper - lower;
            lower = 0;

            int middle = MidPoint(lower, upper);

            if (switchCases[lower].Key[index] == switchCases[middle].Key[index])
            {
                var result = SwitchOnChar(keyParameter, switchCases, index + 1, lower, upper);
                if (result != null)
                {
                    return result;
                }
            }

            middle = GetIndexOfFirstDifferentCaseFromUp(switchCases, lower, middle, upper, switchCase => switchCase.Key[index]);
            if (middle == -1)
            {
                return null;
            }

            var trueBranch = SwitchOnChar(keyParameter, switchCases, index, lower, middle);
            if (trueBranch == null)
            {
                return null;
            }

            var falseBranch = SwitchOnChar(keyParameter, switchCases, index, middle + 1, upper);
            if (falseBranch == null)
            {
                return null;
            }

            return Expression.Condition(
                Expression.LessThan(Expression.Call(keyParameter, StringIndex, Expression.Constant(index)),
                    Expression.Constant(switchCases[middle + 1].Key[index])),
                    trueBranch,
                    falseBranch);
        }

        private static int MidPoint(int lower, int upper)
        {
            return ((upper - lower + 1) / 2) + lower;
        }

        private static int GetIndexOfFirstDifferentCaseFromUp<T>(SwitchCase[] cases, int lower, int middle, int upper, Func<SwitchCase, T> selector)
        {
            T firstValue = selector(cases[middle]);
            for (int i = middle - 1; i >= lower; --i)
            {
                if (!firstValue.Equals(selector(cases[i])))
                {
                    return i;
                }
            }

            for (int i = middle + 1; i <= upper; ++i)
            {
                if (!firstValue.Equals(selector(cases[i])))
                {
                    return i - 1;
                }
            }

            return -1;
        }

        private static readonly MethodInfo StringLength = typeof(string).GetMethod("get_Length");
        private static readonly MethodInfo StringIndex = typeof(string).GetMethod("get_Chars");
        private static readonly MethodInfo StringEquals = typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(string) });

        /// <inheritdoc />
        public void Add(string key, TVal value) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool ContainsKey(string key) => throw new InvalidOperationException();

        /// <inheritdoc />
        public ICollection<string> Keys => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Remove(string key) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool TryGetValue(string key, out TVal value) => throw new InvalidOperationException();

        /// <inheritdoc />
        public ICollection<TVal> Values => throw new InvalidOperationException();

        /// <inheritdoc />
        public TVal this[string key]
        {
            get => string.IsNullOrEmpty(key) ? _fallback(key) : _switchFunction(key);
            set => throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, TVal> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public void Clear() => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, TVal> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, TVal>[] array, int arrayIndex) => throw new InvalidOperationException();

        /// <inheritdoc />
        public int Count => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, TVal> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, TVal>> GetEnumerator() => throw new InvalidOperationException();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class StaticStringDictionaryComparer : IComparer<string>
    {
        private readonly int _startIndex;

        public StaticStringDictionaryComparer(int startIndex)
        {
            _startIndex = startIndex;
        }

        private static readonly Dictionary<int, IComparer<string>> Comparers = new();

        public static IComparer<string> For(int startIndex)
        {
            if (!Comparers.TryGetValue(startIndex, out var comparer))
            {
                comparer = new StaticStringDictionaryComparer(startIndex);
                Comparers.Add(startIndex, comparer);
            }

            return comparer;
        }

        public int Compare(string x, string y)
        {
            if (x.Length != y.Length)
            {
                throw new InvalidOperationException();
            }

            for (int i = _startIndex; i < x.Length; i++)
            {
                if (x[i] > y[i])
                {
                    return 1;
                }

                if (x[i] < y[i])
                {
                    return -1;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// Factory for creating bi-directional compiled switch dictionaries mapping string keys to value types.
    /// </summary>
    public static class DoubleStaticStringDictionary
    {
        /// <summary>
        /// Creates a new <see cref="DoubleStaticStringDictionary{TVal}"/> instance.
        /// </summary>
        public static DoubleStaticStringDictionary<TVal> Create<TVal>(IEnumerable<KeyValuePair<string, TVal>> dict, Func<string, TVal> fallback, Func<TVal, string> reverseFallback)
        {
            return new DoubleStaticStringDictionary<TVal>(dict, fallback, reverseFallback);
        }
    }

    /// <summary>
    /// Bi-directional compiled switch dictionary allowing high-performance string-to-value and value-to-string lookups.
    /// </summary>
    /// <typeparam name="TVal">The value type.</typeparam>
    public class DoubleStaticStringDictionary<TVal> : StaticStringDictionary<TVal>, IDictionary<TVal, string>
    {
        private readonly Func<TVal, string> _reverseFallback;
        private readonly IDictionary<TVal, string> _reverseDict;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleStaticStringDictionary{TVal}"/> class.
        /// </summary>
        public DoubleStaticStringDictionary(IEnumerable<KeyValuePair<string, TVal>> dict, Func<string, TVal> fallback, Func<TVal, string> reverseFallback)
            : base(dict, fallback)
        {
            _reverseFallback = reverseFallback;
            _reverseDict = new Dictionary<TVal, string>();
            foreach (KeyValuePair<string, TVal> pair in dict)
            {
                _reverseDict.Add(pair.Value, pair.Key);
            }
        }

        /// <inheritdoc />
        public void Add(TVal key, string value) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool ContainsKey(TVal key) => throw new InvalidOperationException();

        /// <inheritdoc />
        public new ICollection<TVal> Keys => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Remove(TVal key) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool TryGetValue(TVal key, out string value) => throw new InvalidOperationException();

        /// <inheritdoc />
        public new ICollection<string> Values => throw new InvalidOperationException();

        /// <inheritdoc />
        public string this[TVal key]
        {
            get => _reverseDict.TryGetValue(key, out var result) ? result : _reverseFallback(key);
            set => throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TVal, string> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TVal, string> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TVal, string>[] array, int arrayIndex) => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TVal, string> item) => throw new InvalidOperationException();

        /// <inheritdoc />
        public new IEnumerator<KeyValuePair<TVal, string>> GetEnumerator() => throw new InvalidOperationException();
    }
}
