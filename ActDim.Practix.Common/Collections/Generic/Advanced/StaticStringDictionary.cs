#region License

/*
 * Copyright 2002-2010 Paul Borodaev.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Practix.Collections.Generic.Specialized
{
	public static class StaticStringDictionary
	{

		public static StaticStringDictionary<Type> Create<Type>(IEnumerable<KeyValuePair<string, Type>> dict, Func<string, Type> fallback)
		{
			return new StaticStringDictionary<Type>(dict, fallback);
		}
	}

	public class StaticStringDictionary<Type> : IDictionary<string, Type>
	{

		private readonly Func<string, Type> _fallback;
		private readonly Func<string, Type> _switchFunction;

		public StaticStringDictionary(IEnumerable<KeyValuePair<string, Type>> dict, Func<string, Type> fallback)
		{
			this._fallback = fallback;
			this._switchFunction = CreateSwitch(dict);
		}

		private struct SwitchCase
		{
			public readonly string Key;
			public readonly Type Value;
			public SwitchCase(string key, Type value)
			{
				Key = key;
				Value = value;
			}
			public override string ToString()
			{
				return Key + " " + Value.ToString();
			}
		}

		private Func<string, Type> CreateSwitch(IEnumerable<KeyValuePair<string, Type>> dict)
		{
			var cases = dict.Select(pair => new SwitchCase(pair.Key, pair.Value)).ToList();
			ParameterExpression keyParameter = Expression.Parameter(typeof(string), "key");
			var expr = Expression.Lambda<Func<string, Type>>(
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
				Expression.LessThan(Expression.Call(keyParameter, stringLength), Expression.Constant(switchCases[middle + 1].Key.Length)),
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
					Expression.Call(stringEquals, keyParameter, Expression.Constant(switchCases[lower].Key)),
					Expression.Convert(Expression.Constant(switchCases[lower].Value), typeof(Type)),
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
				Expression.LessThan(Expression.Call(keyParameter, stringIndex, Expression.Constant(index)),
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

		private static MethodInfo stringLength = typeof(string).GetMethod("get_Length");
		private static MethodInfo stringIndex = typeof(string).GetMethod("get_Chars");
		private static MethodInfo stringEquals = typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(string) });

		public void Add(string key, Type value)
		{
			throw new InvalidOperationException();
		}

		public bool ContainsKey(string key)
		{
			throw new InvalidOperationException();
		}

		public ICollection<string> Keys
		{
			get { throw new InvalidOperationException(); }
		}

		public bool Remove(string key)
		{
			throw new InvalidOperationException();
		}

		public bool TryGetValue(string key, out Type value)
		{
			throw new InvalidOperationException();
		}

		public ICollection<Type> Values
		{
			get { throw new InvalidOperationException(); }
		}

		public Type this[string key]
		{
			get { return string.IsNullOrEmpty(key) ? _fallback(key) : _switchFunction(key); }
			set { throw new InvalidOperationException(); }
		}

		public void Add(KeyValuePair<string, Type> item)
		{
			throw new InvalidOperationException();
		}

		public void Clear()
		{
			throw new InvalidOperationException();
		}

		public bool Contains(KeyValuePair<string, Type> item)
		{
			throw new InvalidOperationException();
		}

		public void CopyTo(KeyValuePair<string, Type>[] array, int arrayIndex)
		{
			throw new InvalidOperationException();
		}

		public int Count
		{
			get { throw new InvalidOperationException(); }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(KeyValuePair<string, Type> item)
		{
			throw new InvalidOperationException();
		}

		public IEnumerator<KeyValuePair<string, Type>> GetEnumerator()
		{
			throw new InvalidOperationException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new InvalidOperationException();
		}
	}

	internal class StaticStringDictionaryComparer : IComparer<string>
	{

		private readonly int startIndex;
		public StaticStringDictionaryComparer(int startIndex)
		{
			this.startIndex = startIndex;
		}

		private static Dictionary<int, IComparer<string>> comparers = new Dictionary<int, IComparer<string>>();

		public static IComparer<string> For(int startIndex)
		{
			IComparer<string> comparer;
			if (!comparers.TryGetValue(startIndex, out comparer))
			{
				comparer = new StaticStringDictionaryComparer(startIndex);
				comparers.Add(startIndex, comparer);
			}
			return comparer;
		}

		public int Compare(string x, string y)
		{

			if (x.Length != y.Length)
			{
				throw new InvalidOperationException();
			}

			for (int i = startIndex; i < x.Length; i++)
			{
				if (x[i] > y[i])
				{
					return 1;
				}
				else if (x[i] < y[i])
				{
					return -1;
				}
			}

			return 0;
		}
	}

	public static class DoubleStaticStringDictionary
	{

		public static DoubleStaticStringDictionary<Type> Create<Type>(IEnumerable<KeyValuePair<string, Type>> dict, Func<string, Type> fallback, Func<Type, string> reverseFallback)
		{
			return new DoubleStaticStringDictionary<Type>(dict, fallback, reverseFallback);
		}
	}

	public class DoubleStaticStringDictionary<Type> : StaticStringDictionary<Type>, IDictionary<Type, string>
	{

		private Func<Type, string> reverseFallback;
		private IDictionary<Type, string> reverseDict;

		public DoubleStaticStringDictionary(IEnumerable<KeyValuePair<string, Type>> dict, Func<string, Type> fallback, Func<Type, string> reverseFallback)
			: base(dict, fallback)
		{

			this.reverseFallback = reverseFallback;

			reverseDict = new Dictionary<Type, string>();
			foreach (KeyValuePair<string, Type> pair in dict)
			{
				reverseDict.Add(pair.Value, pair.Key);
			}
		}

		public void Add(Type key, string value)
		{
			throw new InvalidOperationException();
		}

		public bool ContainsKey(Type key)
		{
			throw new InvalidOperationException();
		}

		public new ICollection<Type> Keys
		{
			get { throw new InvalidOperationException(); }
		}

		public bool Remove(Type key)
		{
			throw new InvalidOperationException();
		}

		public bool TryGetValue(Type key, out string value)
		{
			throw new InvalidOperationException();
		}

		public new ICollection<string> Values
		{
			get { throw new InvalidOperationException(); }
		}

		public string this[Type key]
		{
			get
			{
				string result;
				if (reverseDict.TryGetValue(key, out result))
				{
					return result;
				}
				else
				{
					return reverseFallback(key);
				}
			}
			set { throw new InvalidOperationException(); }
		}

		public void Add(KeyValuePair<Type, string> item)
		{
			throw new InvalidOperationException();
		}

		public bool Contains(KeyValuePair<Type, string> item)
		{
			throw new InvalidOperationException();
		}

		public void CopyTo(KeyValuePair<Type, string>[] array, int arrayIndex)
		{
			throw new InvalidOperationException();
		}

		public bool Remove(KeyValuePair<Type, string> item)
		{
			throw new InvalidOperationException();
		}

		public new IEnumerator<KeyValuePair<Type, string>> GetEnumerator()
		{
			throw new InvalidOperationException();
		}
	}
}
