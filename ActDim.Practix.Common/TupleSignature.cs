#region License

/*
 * Copyright � 2002-2011 Paul Borodaev.
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

#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.GuardClauses;
using System.Reflection;

#endregion

namespace ActDim.Practix
{
	public sealed class TupleSignature : IEquatable<TupleSignature>
	{
		private readonly object[] _items;

		public object[] Items { get { return _items; } }

		private readonly int _hashCode;
		
		public TupleSignature(params object[] items)
		{
			Guard.Against.Null(items, nameof(items)); // .IsNotEmpty()
			_items = items;
			// _items = (object[])items.Clone();
			// _items = new object[items.Length];						
			// Array.Copy(items, 0, _items, 0, items.Length);
			_hashCode = HashCodeHelper.CombineHashCode(_items);
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TupleSignature);
		}

		public bool Equals(TupleSignature other)
		{
			// Check for NULL
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			// Check for same reference
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (Items.Length != other.Items.Length)
			{
				return false;
			}

			return Items.SequenceEqual(other.Items);
		}
	}
}