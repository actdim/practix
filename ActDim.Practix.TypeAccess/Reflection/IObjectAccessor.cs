using System;
#if NET_2
using System.Collections.ObjectModel;
using System.Collections.Generic;
#endif

namespace ActDim.Practix.TypeAccess.Reflection
{
	public interface IObjectAccessor<T> where T: class
	{
		T Instance { get; }		

		//Func<T, TOutput> GetPropertyOrFieldGetter<TOutput>(string name); // TODO

		TProperty GetProperty<TProperty>(string name);

		Func<T, TProperty> GetPropertyGetter<TProperty>(string name);

		TField GetField<TField>(string name);

		Func<T, TField> GetFieldGetter<TField>(string name);

		TDelegate GetMethodCaller<TDelegate>(string name);
	}
}
