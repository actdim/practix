using System;
using System.Text.Json;

namespace ActDim.Practix.Common.Json
{
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Gets the value of a standard value type from a JsonElement.
        /// Returns null if the element is Null or Undefined.
        /// Supported types: int, long, short, uint, ulong, ushort, byte, sbyte,
        /// double, float, decimal, bool, Guid, DateTime, DateTimeOffset.
        /// </summary>
        public static T? GetValue<T>(this JsonElement element) where T : struct
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;

            if (typeof(T) == typeof(int)) return (T)(object)element.GetInt32();
            if (typeof(T) == typeof(long)) return (T)(object)element.GetInt64();
            if (typeof(T) == typeof(short)) return (T)(object)element.GetInt16();
            if (typeof(T) == typeof(uint)) return (T)(object)element.GetUInt32();
            if (typeof(T) == typeof(ulong)) return (T)(object)element.GetUInt64();
            if (typeof(T) == typeof(ushort)) return (T)(object)element.GetUInt16();
            if (typeof(T) == typeof(byte)) return (T)(object)element.GetByte();
            if (typeof(T) == typeof(sbyte)) return (T)(object)element.GetSByte();
            if (typeof(T) == typeof(double)) return (T)(object)element.GetDouble();
            if (typeof(T) == typeof(float)) return (T)(object)element.GetSingle();
            if (typeof(T) == typeof(decimal)) return (T)(object)element.GetDecimal();
            if (typeof(T) == typeof(bool)) return (T)(object)element.GetBoolean();
            if (typeof(T) == typeof(Guid)) return (T)(object)element.GetGuid();
            if (typeof(T) == typeof(DateTime)) return (T)(object)element.GetDateTime();
            if (typeof(T) == typeof(DateTimeOffset)) return (T)(object)element.GetDateTimeOffset();

            throw new NotSupportedException($"Type {typeof(T).Name} is not supported");
        }

        /// <summary>
        /// Gets a property by name (case-insensitive) from a JsonElement of kind Object.
        /// Returns null if not found or if the element is not an Object.
        /// </summary>
        public static JsonElement? GetPropertyCI(this JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }

            return null;
        }
    }
}
