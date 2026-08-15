using System;

namespace ActDim.Practix.TypeAccess.Reflection
{
	/// <summary>
	/// Provides typed reflection access to properties, fields, and methods of an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The object type.</typeparam>
	public interface IObjectAccessor<T> where T : class
	{
		/// <summary>
		/// Gets the underlying target instance.
		/// </summary>
		T Instance { get; }

		/// <summary>
		/// Gets the value of the specified property.
		/// </summary>
		TProperty GetProperty<TProperty>(string name);

		/// <summary>
		/// Gets a getter delegate for the specified property.
		/// </summary>
		Func<T, TProperty> GetPropertyGetter<TProperty>(string name);

		/// <summary>
		/// Gets the value of the specified field.
		/// </summary>
		TField GetField<TField>(string name);

		/// <summary>
		/// Gets a getter delegate for the specified field.
		/// </summary>
		Func<T, TField> GetFieldGetter<TField>(string name);

		/// <summary>
		/// Gets a method invoker delegate for the specified method.
		/// </summary>
		TDelegate GetMethodCaller<TDelegate>(string name);
	}
}
