using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Type"/> introspection and default value inspection.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly HashSet<Type> PrimitiveTypes;
        private static readonly ConcurrentDictionary<Type, object> Defaults = new();

        static TypeExtensions()
        {
            var types = new[]
            {
                typeof(Enum),
                typeof(string),
                typeof(char),
                typeof(Guid),
                typeof(bool),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(sbyte),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
            };

            var nullTypes = types.Where(t => t.IsValueType).Select(t => typeof(Nullable<>).MakeGenericType(t));
            var arrayTypes = new[] { typeof(byte[]) };
            PrimitiveTypes = new HashSet<Type>(types.Concat(nullTypes).Concat(arrayTypes));
        }

        /// <summary>
        /// Determines whether a type is a simple type (e.g. string, enum, primitive numbers, date types, or their nullable variants).
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True if simple; otherwise, false.</returns>
        public static bool IsSimple(this Type type)
        {
            if (PrimitiveTypes.Any(x => x.IsAssignableFrom(type)) || type == typeof(object))
            {
                return true;
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            return nullableType != null && nullableType.IsEnum;
        }

        /// <summary>
        /// Gets the default value for the specified runtime type (e.g. <c>0</c> for <see cref="int"/>, <c>null</c> for reference types).
        /// </summary>
        /// <param name="type">The runtime type.</param>
        /// <returns>The default value object for the type.</returns>
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
