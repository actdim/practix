using System;

namespace ActDim.Practix.TypeAccess.Reflection
{
	/// <summary>
	/// Provides fast reflection access to properties, fields, and methods for a specific object instance.
	/// </summary>
	/// <typeparam name="T">The object type.</typeparam>
	public class ObjectAccessor<T> : IObjectAccessor<T> where T : class
	{
		private readonly WeakReference<T> _instanceWeakRef;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectAccessor{T}"/> class wrapping the specified instance.
		/// </summary>
		/// <param name="instance">The target instance.</param>
		public ObjectAccessor(T instance)
		{
			_instanceWeakRef = new WeakReference<T>(instance);
		}

		/// <inheritdoc />
		public T Instance
		{
			get
			{
				if (_instanceWeakRef.TryGetTarget(out var instance))
				{
					return instance;
				}

				throw new ReflectionException("Can't access target object");
			}
		}

		/// <inheritdoc />
		public TProperty GetProperty<TProperty>(string name)
		{
			return TypeAccessor<T>.GetPropertyGetter<TProperty>(name)(Instance);
		}

		/// <inheritdoc />
		public Func<T, TProperty> GetPropertyGetter<TProperty>(string name)
		{
			return TypeAccessor<T>.GetPropertyGetter<TProperty>(name);
		}

		/// <inheritdoc />
		public TField GetField<TField>(string name)
		{
			return TypeAccessor<T>.GetFieldGetter<TField>(name)(Instance);
		}

		/// <inheritdoc />
		public Func<T, TField> GetFieldGetter<TField>(string name)
		{
			return TypeAccessor<T>.GetFieldGetter<TField>(name);
		}

		/// <inheritdoc />
		public TDelegate GetMethodCaller<TDelegate>(string name)
		{
			return TypeAccessor<T>.GetMethodCaller<TDelegate>(name);
		}
	}
}
