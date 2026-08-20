using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Common.Memory;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="string"/> manipulation, encoding, and stream conversions.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex _identifierRegex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]", RegexOptions.Compiled);

        private static Encoding DefaultEncoding { get; } = new UTF8Encoding(false); // encoding without preamble!

        /// <summary>
        /// Sanitizes an arbitrary string input into a valid C# identifier name.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>A valid C# identifier string.</returns>
        public static string ToCSharpIdentifier(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "_empty";
            }

            string result = _identifierRegex.Replace(input, "_");

            if (!char.IsLetter(result[0]) && result[0] != '_')
            {
                result = "_" + result;
            }

            return result;
        }

        /// <summary>
        /// Determines whether a string contains a specified substring using the given comparison type.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="value">The substring to locate.</param>
        /// <param name="comparisonType">The comparison rule.</param>
        /// <returns>True if value is contained; otherwise, false.</returns>
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source.IndexOf(value, comparisonType) >= 0;
        }

        /// <summary>
        /// Indicates whether the specified string is null or an empty string ("").
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>True if value is null or empty; otherwise, false.</returns>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Encodes a string asynchronously into a pre-sized pooled <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="value">The string value to encode.</param>
        /// <param name="encoding">The encoding to use (defaults to UTF-8 without preamble).</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A seekable <see cref="MemoryStream"/> positioned at offset 0.</returns>
        public static async Task<MemoryStream> ToMemoryAsync(this string value, Encoding encoding = default, CancellationToken ct = default)
        {
            if (encoding == default)
            {
                encoding = DefaultEncoding;
            }

            var length = encoding.GetByteCount(value);

            // Pre-sized so the stream does not have to chain extra blocks while being written.
            var stream = MemoryManager.Default.GetStream(nameof(StringExtensions), length);

            await stream.WriteStringAsync(value, encoding, ct);

            // Hand back a stream ready to be read, not one parked at the end of what was just
            // written. Keep this in step with the synchronous ToMemory.
            stream.Position = 0L;

            return stream;
        }

        /// <summary>
        /// Encodes a string into a pre-sized pooled <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="value">The string value to encode.</param>
        /// <param name="encoding">The encoding to use (defaults to UTF-8 without preamble).</param>
        /// <returns>A seekable <see cref="MemoryStream"/> positioned at offset 0.</returns>
        public static MemoryStream ToMemory(this string value, Encoding encoding = default)
        {
            if (encoding == default)
            {
                encoding = DefaultEncoding;
            }

            var length = encoding.GetByteCount(value);

            // Pre-sized so the stream does not have to chain extra blocks while being written.
            var stream = MemoryManager.Default.GetStream(nameof(StringExtensions), length);

            // Encoding straight into the stream to avoid staging bytes into an intermediate
            // buffer first (which would copy bytes twice).
            stream.WriteString(value, encoding);

            // Hand back a stream ready to be read, not one parked at the end of what was just
            // written. Keep this in step with ToMemoryAsync.
            stream.Position = 0L;

            return stream;
        }

        /// <summary>
        /// Splits a string by delimiter while respecting quoted qualifier boundaries.
        /// </summary>
        /// <param name="expression">The expression to split.</param>
        /// <param name="delimiter">The delimiter string.</param>
        /// <param name="qualifier">The quote/qualifier string (defaults to quote).</param>
        /// <returns>An array of split tokens.</returns>
        public static string[] Split(this string expression, string delimiter, string qualifier)
        {
            return Split(expression, delimiter, qualifier, true);
        }

        /// <summary>
        /// Splits a string by delimiter while respecting double-quote qualifier boundaries.
        /// </summary>
        /// <param name="expression">The expression to split.</param>
        /// <param name="delimiter">The delimiter string.</param>
        /// <returns>An array of split tokens.</returns>
        public static string[] Split(this string expression, string delimiter)
        {
            return Split(expression, delimiter, "\"", true);
        }

        /// <summary>
        /// Splits a string by delimiter while respecting quote qualifier boundaries and case-insensitivity.
        /// </summary>
        /// <param name="expression">The expression to split.</param>
        /// <param name="delimiter">The delimiter string.</param>
        /// <param name="qualifier">The quote/qualifier string.</param>
        /// <param name="ignoreCase">Whether to ignore case when matching delimiters.</param>
        /// <returns>An array of split tokens.</returns>
        public static string[] Split(this string expression, string delimiter, string qualifier, bool ignoreCase)
        {
            qualifier ??= "\"";
            string statement = string.Format("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))", Regex.Escape(delimiter), Regex.Escape(qualifier));
            //\s+(?=(?:[^"]*"[^"]*")*(?![^"]*"))

            var options = RegexOptions.Multiline;
            if (ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return new Regex(statement, options).Split(expression);
        }
    }
}
