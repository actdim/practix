using Autofac;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class TypeExtensions
    {
        private static readonly HashSet<Type> PrimitiveTypes;
        private static readonly ConcurrentDictionary<Type, object> Defaults =
            new ConcurrentDictionary<Type, object>();

        static TypeExtensions()
        {
            // primitive types: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, Single

            var types = new[]
            {
                typeof (Enum),
                typeof (string),
                typeof (char),
                typeof (Guid),

                typeof (bool),
                typeof (byte),
                typeof (short),
                typeof (int),
                typeof (long),
                typeof (float),
                typeof (double),
                typeof (decimal),

                typeof (sbyte),
                typeof (ushort),
                typeof (uint),
                typeof (ulong),

                typeof (DateTime),
                typeof (DateTimeOffset),
                typeof (TimeSpan),
            };

            var nullTypes = types.Where(t => t.IsValueType).Select(t => typeof(Nullable<>).MakeGenericType(t));
            var arrayTypes = new[] { typeof(byte[]) };
            PrimitiveTypes = new HashSet<Type>(types.Concat(nullTypes).Concat(arrayTypes));
        }

        /// <summary>
        /// Returns true if type is simple
        /// (like string, Enum, int, long etc) including nullable types as well
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSimple(this Type type)
        {
            if (PrimitiveTypes.Any(x => x.IsAssignableFrom(type)) ||
                type == typeof(object))
            {
                return true;
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            return nullableType != null && nullableType.IsEnum;
        }

        public static object GetDefaultValue(this Type type)
        {
            return Defaults.GetOrAdd(type, t =>
            {
                var defaultExpr = Expression.Default(t);
                return Expression.Lambda(defaultExpr).Compile().DynamicInvoke();
            });
        }
    }
}