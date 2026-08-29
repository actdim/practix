using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Factory that creates converters for types that have implicit operators to primitive types.
    /// </summary>
    public class ImplicitOperatorConverterFactory : JsonConverterFactory
    {
        private static readonly Type[] TargetPrimitives =
        [
            // Boolean
            typeof(bool),
            // Integer (narrow to wide)
            typeof(int),
            typeof(long),
            // Floating point (narrow to wide)
            typeof(float),
            typeof(double),
            typeof(decimal),
            // Date/time (DateTime has no timezone, DateTimeOffset is wider)
            typeof(DateTime),
            typeof(DateTimeOffset),
            // String - widest, last
            typeof(string),
        ];

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.IsPrimitive || typeToConvert == typeof(string))
            {
                return false;
            }

            if (typeToConvert == typeof(DateTime) || typeToConvert == typeof(DateTimeOffset))
            {
                return false;
            }

            if (typeof(JsonNode).IsAssignableFrom(typeToConvert))
            {
                return false;
            }

            return FindImplicitToPrimitive(typeToConvert) != null;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var (method, targetType) = FindImplicitToPrimitive(typeToConvert)!.Value;

            var converterType = typeof(ImplicitOperatorConverter<,>)
                .MakeGenericType(typeToConvert, targetType);

            return (JsonConverter)Activator.CreateInstance(converterType, method)!;
        }

        internal static (MethodInfo method, Type targetType)? FindImplicitToPrimitive(Type sourceType)
        {
            foreach (var target in TargetPrimitives)
            {
                var method = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                        m.ReturnType == target &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == sourceType);

                if (method != null)
                {
                    return (method, target);
                }
            }

            return null;
        }
    }

    internal class ImplicitOperatorConverter<TSource, TPrimitive> : JsonConverter<TSource>
    {
        private readonly MethodInfo _implicitToPrimitive;
        private readonly MethodInfo _implicitFromPrimitive;

        public ImplicitOperatorConverter(MethodInfo implicitToPrimitive)
        {
            _implicitToPrimitive = implicitToPrimitive;
            _implicitFromPrimitive = typeof(TSource)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                    m.ReturnType == typeof(TSource) &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(TPrimitive));
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TSource value, JsonSerializerOptions options)
        {
            var converted = (TPrimitive)_implicitToPrimitive.Invoke(null, [value])!;

            switch (converted)
            {
                case DateTime dt:
                    writer.WriteStringValue(dt);
                    break;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto);
                    break;
                default:
                    JsonSerializer.Serialize(writer, converted, options);
                    break;
            }
        }

        /// <inheritdoc />
        public override TSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            TPrimitive primitive;

            if (typeof(TPrimitive) == typeof(DateTime))
            {
                primitive = (TPrimitive)(object)reader.GetDateTime();
            }
            else if (typeof(TPrimitive) == typeof(DateTimeOffset))
            {
                primitive = (TPrimitive)(object)reader.GetDateTimeOffset();
            }
            else
            {
                primitive = JsonSerializer.Deserialize<TPrimitive>(ref reader, options)!;
            }

            if (_implicitFromPrimitive != null)
            {
                return (TSource)_implicitFromPrimitive.Invoke(null, [primitive])!;
            }

            throw new JsonException(
                $"No implicit/explicit operator from {typeof(TPrimitive)} to {typeToConvert}. " +
                $"Add: public static implicit operator {typeToConvert.Name}({typeof(TPrimitive).Name} value)");
        }
    }
}
