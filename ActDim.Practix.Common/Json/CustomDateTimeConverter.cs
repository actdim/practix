using ActDim.Practix.Common.DataFormat;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// Converts naive (timezone-agnostic) <see cref="DateTime"/> values using <see cref="DateTimeFormatConstants.NaiveDateTimeFormat"/>.
    /// </summary>
    public class NaiveDateTimeConverter : CustomDateTimeConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NaiveDateTimeConverter"/> class.
        /// </summary>
        public NaiveDateTimeConverter() : base([DateTimeFormatConstants.NaiveDateTimeFormat])
        {
        }
    }

    /// <summary>
    /// Configurable <see cref="DateTime"/> converter supporting custom input/output format strings, cultures, and <see cref="DateTimeKind"/>.
    /// </summary>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DefaultDateTimeFormat = DateTimeFormatConstants.UtcDateTimeFormat;
        private readonly string[] _inputFormats;
        private readonly string _outputFormat;
        private readonly DateTimeKind _kind;
        private readonly CultureInfo _inputCulture;
        private readonly CultureInfo _outputCulture;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDateTimeConverter"/> class with default settings.
        /// </summary>
        public CustomDateTimeConverter() : this(default, default, default, default, default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDateTimeConverter"/> class with custom format, culture, and kind settings.
        /// </summary>
        /// <param name="inputFormats">Supported input format strings.</param>
        /// <param name="outputFormat">Target output format string.</param>
        /// <param name="inputCulture">Culture for parsing input strings.</param>
        /// <param name="outputCulture">Culture for formatting output strings.</param>
        /// <param name="kind">Target <see cref="DateTimeKind"/>.</param>
        public CustomDateTimeConverter(string[] inputFormats, string outputFormat = default, CultureInfo inputCulture = default, CultureInfo outputCulture = default, DateTimeKind kind = default)
        {
            _inputFormats = inputFormats ?? [DefaultDateTimeFormat];
            _inputCulture = inputCulture ?? CultureInfo.InvariantCulture;
            _outputFormat = outputFormat ?? _inputFormats.FirstOrDefault() ?? DefaultDateTimeFormat;
            _outputCulture = outputCulture ?? _inputCulture;
            _kind = kind;
        }

        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (!DateTimeOffset.TryParseExact(str, _inputFormats, _inputCulture, DateTimeStyles.None, out var dto))
                {
                    if (!DateTimeOffset.TryParse(str, _inputCulture, DateTimeStyles.None, out dto))
                    {
                        throw new JsonException($"Cannot read DateTime value from string \"{str}\".");
                    }
                }

                var date = dto.DateTime;
                date = DateTime.SpecifyKind(date, _kind);
                return date;
            }

            throw new JsonException($"Cannot read DateTime value from {Enum.GetName(reader.TokenType)}.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_outputFormat, _outputCulture.DateTimeFormat));
        }
    }
}
