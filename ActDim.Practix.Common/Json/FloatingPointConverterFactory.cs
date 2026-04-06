using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// Global factory that serializes double/float values with a decimal point for whole numbers
    /// (e.g. 7.0 instead of 7), matching Newtonsoft.Json's default floating-point behavior.
    /// Handles non-finite double values (NaN, Infinity, -Infinity) as strings,
    /// matching legacy Newtonsoft FloatFormatHandling.String behavior.
    /// <summary>    
    public class FloatingPointConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(double) || typeToConvert == typeof(double?) ||
            typeToConvert == typeof(float) || typeToConvert == typeof(float?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            typeToConvert == typeof(double) ? new DoubleConverter() :
            typeToConvert == typeof(double?) ? new NullableDoubleConverter() :
            typeToConvert == typeof(float) ? new FloatConverter() : new NullableFloatConverter();

        private static readonly NumberFormatInfo Nfi = new() { NumberDecimalSeparator = "." };

        private static string Format(double value)
        {
            var s = value.ToString("R", Nfi);
            if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
                s += ".0";
            return s;
        }

        private static string Format(float value)
        {
            var s = value.ToString("R", Nfi);
            if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
                s += ".0";
            return s;
        }

        public static double ReadDouble(ref Utf8JsonReader reader)
        {
            var s = reader.GetString();
            return s switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                _ => double.Parse(s, CultureInfo.InvariantCulture)
            };
        }

        public static void WriteDouble(Utf8JsonWriter writer, double value)
        {
            if (double.IsNaN(value))
                writer.WriteStringValue("NaN");
            else if (double.IsPositiveInfinity(value))
                writer.WriteStringValue("Infinity");
            else if (double.IsNegativeInfinity(value))
                writer.WriteStringValue("-Infinity");
            else
                writer.WriteRawValue(Format(value));
        }

        private sealed class DoubleConverter : JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return ReadDouble(ref reader);
                }
                return reader.GetDouble();
            }

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                WriteDouble(writer, value);
            }
        }

        private sealed class NullableDoubleConverter : JsonConverter<double?>
        {
            public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return ReadDouble(ref reader);
                }
                return reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
            }

            public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
            {
                if (value is null)
                    writer.WriteNullValue();
                else
                    WriteDouble(writer, (double)value);
            }
        }

        private sealed class FloatConverter : JsonConverter<float>
        {
            public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => reader.GetSingle();

            public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
                => writer.WriteRawValue(Format(value));
        }

        private sealed class NullableFloatConverter : JsonConverter<float?>
        {
            public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();

            public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
            {
                if (value is null)
                    writer.WriteNullValue();
                else
                    writer.WriteRawValue(Format(value.Value));
            }
        }
    }
}
