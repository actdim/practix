using Conditions;
using ActDim.Practix.TypeAccess.Reflection;

namespace ActDim.Practix.TypeAccess.Linq //System.Reflection/ActDim.Practix.TypeAccess.Extensions/ActDim.Practix.TypeAccess.Reflection.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Use ReflectionHelper<T> as Type accessor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IObjectAccessor<T> GetAccessor<T>(this T obj) where T : class
        {
            return new ObjectAccessor<T>(obj);
        }

        public static TDelegate GetPropertyGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate // <TProperty>
        {
            obj.Requires(nameof(obj)).IsNotNull();
            return obj.GetType().GetPropertyGetter<TDelegate>(name);
        }

        public static TDelegate GetFieldGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate // <TProperty>
        {
            obj.Requires(nameof(obj)).IsNotNull();
            return obj.GetType().GetFieldGetter<TDelegate>(name) as TDelegate;
        }

        public static TProperty GetProperty<TProperty>(this object obj, string name)
        {
            obj.Requires(nameof(obj)).IsNotNull();
            var getter = TypeAccessor.GetPropertyGetter(obj.GetType(), name);
            return (TProperty)getter.DynamicInvoke(obj);
        }

        public static TField GetField<TField>(this object obj, string name)
        {
            obj.Requires(nameof(obj)).IsNotNull();
            var getter = TypeAccessor.GetFieldGetter(obj.GetType(), name);
            return (TField)getter.DynamicInvoke(obj);
        }

        // GetMethodInvoker
        public static TDelegate GetMethodCaller<TDelegate>(this object obj, string name)
        {
            obj.Requires(nameof(obj)).IsNotNull();
            return obj.GetType().GetMethodCaller<TDelegate>(name);
        }

        // TODO: add other extension methods (based on ReflectionHelper)
    }
}