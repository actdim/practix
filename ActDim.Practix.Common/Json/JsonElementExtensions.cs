using System;
using System.Text.Json;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// Extension methods for <see cref="JsonElement"/> property access and strongly typed conversions.
    /// </summary>
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Gets the value of a standard value type from a <see cref="JsonElement"/>.
        /// Returns null if the element is Null or Undefined.
        /// Supported types: int, long, short, uint, ulong, ushort, byte, sbyte,
        /// double, float, decimal, bool, Guid, DateTime, DateTimeOffset.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="element">The JSON element.</param>
        /// <returns>The parsed value, or null if element is null/undefined.</returns>
        public static T? GetValue<T>(this JsonElement element) where T : struct
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            var type = typeof(T);

            if (type == typeof(int))
            {
                return (T)(object)element.GetInt32();
            }

            if (type == typeof(long))
            {
                return (T)(object)element.GetInt64();
            }

            if (type == typeof(short))
            {
                return (T)(object)element.GetInt16();
            }

            if (type == typeof(uint))
            {
                return (T)(object)element.GetUInt32();
            }

            if (type == typeof(ulong))
            {
                return (T)(object)element.GetUInt64();
            }

            if (type == typeof(ushort))
            {
                return (T)(object)element.GetUInt16();
            }

            if (type == typeof(byte))
            {
                return (T)(object)element.GetByte();
            }

            if (type == typeof(sbyte))
            {
                return (T)(object)element.GetSByte();
            }

            if (type == typeof(double))
            {
                return (T)(object)element.GetDouble();
            }

            if (type == typeof(float))
            {
                return (T)(object)element.GetSingle();
            }

            if (type == typeof(decimal))
            {
                return (T)(object)element.GetDecimal();
            }

            if (type == typeof(bool))
            {
                return (T)(object)element.GetBoolean();
            }

            if (type == typeof(Guid))
            {
                return (T)(object)element.GetGuid();
            }

            if (type == typeof(DateTime))
            {
                return (T)(object)element.GetDateTime();
            }

            if (type == typeof(DateTimeOffset))
            {
                return (T)(object)element.GetDateTimeOffset();
            }

            throw new NotSupportedException($"Type {typeof(T).Name} is not supported");
        }

        /// <summary>
        /// Gets a property by name (case-insensitive) from a <see cref="JsonElement"/> of kind Object.
        /// Returns null if not found or if the element is not an Object.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The property value if found; otherwise, null.</returns>
        public static JsonElement? GetPropertyCI(this JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value;
                }
            }

            return null;
        }
    }
}
