using System;
#if NET_2
using System.Collections.ObjectModel;
using System.Collections.Generic;
#endif

namespace ActDim.Practix.TypeAccess.Reflection
{
	public class ObjectAccessor<T> : IObjectAccessor<T> where T : class
	{
		//private T _instance;
		
		public T Instance {
			get
			{
				// return _instance;

				if (InstanceWeekRef.TryGetTarget(out var instance))
				{
					return instance;
				}
				// TargetInvocationException
				throw new ReflectionException("Can't access target object");
			}
		}

		private readonly WeakReference<T> InstanceWeekRef;

		public ObjectAccessor(T instance) 
		{
			// _instance = instance;
			InstanceWeekRef = new WeakReference<T>(instance);
		}

		public TProperty GetProperty<TProperty>(string name)
		{
			return TypeAccessor<T>.GetPropertyGetter<TProperty>(name)(Instance);						
		}

		public Func<T, TProperty> GetPropertyGetter<TProperty>(string name)
		{
			return TypeAccessor<T>.GetPropertyGetter<TProperty>(name);
		}

		public TField GetField<TField>(string name)
		{
			return TypeAccessor<T>.GetFieldGetter<TField>(name)(Instance);			
		}

		public Func<T, TField> GetFieldGetter<TField>(string name)
		{
			return TypeAccessor<T>.GetFieldGetter<TField>(name);
		}		

		public TDelegate GetMethodCaller<TDelegate>(string name)
		{
			return TypeAccessor<T>.GetMethodCaller<TDelegate>(name);
		}
	}
}