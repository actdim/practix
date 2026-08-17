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
    /// </summary>
    public class FloatingPointConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(double) || typeToConvert == typeof(double?) ||
                   typeToConvert == typeof(float) || typeToConvert == typeof(float?);
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(double))
            {
                return new DoubleConverter();
            }

            if (typeToConvert == typeof(double?))
            {
                return new NullableDoubleConverter();
            }

            if (typeToConvert == typeof(float))
            {
                return new FloatConverter();
            }

            return new NullableFloatConverter();
        }

        private static readonly NumberFormatInfo Nfi = new() { NumberDecimalSeparator = "." };

        private static string Format(double value)
        {
            var s = value.ToString("R", Nfi);
            if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
            {
                s += ".0";
            }

            return s;
        }

        private static string Format(float value)
        {
            if (float.IsNaN(value))
            {
                return "NaN";
            }

            if (float.IsPositiveInfinity(value))
            {
                return "Infinity";
            }

            if (float.IsNegativeInfinity(value))
            {
                return "-Infinity";
            }

            var s = value.ToString("R", Nfi);
            if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
            {
                s += ".0";
            }

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
            {
                writer.WriteStringValue("NaN");
            }
            else if (double.IsPositiveInfinity(value))
            {
                writer.WriteStringValue("Infinity");
            }
            else if (double.IsNegativeInfinity(value))
            {
                writer.WriteStringValue("-Infinity");
            }
            else
            {
                writer.WriteRawValue(Format(value));
            }
        }

        private sealed class DoubleConverter : JsonConverter<double>
        {
            /// <inheritdoc />
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return ReadDouble(ref reader);
                }

                return reader.GetDouble();
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                WriteDouble(writer, value);
            }
        }

        private sealed class NullableDoubleConverter : JsonConverter<double?>
        {
            /// <inheritdoc />
            public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return ReadDouble(ref reader);
                }

                return reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    WriteDouble(writer, (double)value);
                }
            }
        }

        private sealed class FloatConverter : JsonConverter<float>
        {
            /// <inheritdoc />
            public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    return s switch
                    {
                        "Infinity" => float.PositiveInfinity,
                        "-Infinity" => float.NegativeInfinity,
                        "NaN" => float.NaN,
                        _ => float.Parse(s, CultureInfo.InvariantCulture)
                    };
                }

                return reader.GetSingle();
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
            {
                writer.WriteRawValue(Format(value));
            }
        }

        private sealed class NullableFloatConverter : JsonConverter<float?>
        {
            /// <inheritdoc />
            public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteRawValue(Format(value.Value));
                }
            }
        }
    }
}
