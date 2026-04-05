using ActDim.Practix.Common.DataFormat;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// WallClockDateTimeConverter
    /// </summary>
    public class NaiveDateTimeConverter : CustomDateTimeConverter
    {
        public NaiveDateTimeConverter() : base([DateTimeFormatConstants.NaiveDateTimeFormat])
        {

        }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DefaultDateTimeFormat = DateTimeFormatConstants.UtcDateTimeFormat;
        private readonly string[] _inputFormats;
        private readonly string _outputFormat;
        private readonly DateTimeKind _kind;
        private readonly CultureInfo _inputCulture;
        private readonly CultureInfo _outputCulture;
        public CustomDateTimeConverter() : this(default, default, default, default, default)
        {
        }
        public CustomDateTimeConverter(string[] inputFormats, string outputFormat = default, CultureInfo inputCulture = default, CultureInfo outputCulture = default, DateTimeKind kind = default)
        {
            _inputFormats = inputFormats ?? [DefaultDateTimeFormat];
            _inputCulture = inputCulture ?? CultureInfo.InvariantCulture;
            _outputFormat = outputFormat ?? _inputFormats.FirstOrDefault() ?? DefaultDateTimeFormat;
            _outputCulture = outputCulture ?? _inputCulture;
            _kind = kind;
        }
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (!DateTimeOffset.TryParseExact(str, _inputFormats, _inputCulture, DateTimeStyles.None, out var dto))
                {
                    if (!DateTimeOffset.TryParse(str, _inputCulture, DateTimeStyles.None, out dto))
                        throw new JsonException($"Cannot read DateTime value from string \"{str}\".");
                }

                var date = dto.DateTime; // Kind = Unspecified
                date = DateTime.SpecifyKind(date, _kind);
                return date;
            }
            throw new JsonException($"Cannot read DateTime value from {Enum.GetName(reader.TokenType)}.");
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_outputFormat, _outputCulture.DateTimeFormat));
        }
    }
}
