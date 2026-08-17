using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Memory;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    /// <summary>
    /// String and StringBuilder extensions
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex _identifierRegex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]", RegexOptions.Compiled);

        public static string ToCSharpIdentifier(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "_empty";

            string result = _identifierRegex.Replace(input, "_");

            if (!char.IsLetter(result[0]) && result[0] != '_')
                result = "_" + result;

            return result;
        }

        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source.IndexOf(value, comparisonType) >= 0;
        }

        public static bool IsNullOrEmpty(this string value)
        {
            //return string.IsNullOrEmpty(value);
            return (value == null || value.Length == 0);
        }

        public static async Task<MemoryStream> ToMemoryAsync(this string value, Encoding encoding = default, CancellationToken ct = default)
        {
            if (encoding == default)
            {
                encoding = DefaultEncoding;
            }

            var length = encoding.GetByteCount(value);

            // Pre-sized so the stream does not have to chain extra blocks while being written.
            var stream = MemoryManager.Default.GetContextStream(length);

            await stream.WriteStringAsync(value, encoding, ct);

            // Hand back a stream ready to be read, not one parked at the end of what was just
            // written. Keep this in step with the synchronous ToMemory.
            stream.Position = 0L;

            return stream;
        }

        private static Encoding DefaultEncoding { get; } = new UTF8Encoding(false); // encoding without preamble!

        public static MemoryStream ToMemory(this string value, Encoding encoding = default) // int bufferSize = BufferSize
        {
            if (encoding == default)
            {
                encoding = DefaultEncoding;
            }

            var length = encoding.GetByteCount(value);

            // Pre-sized so the stream does not have to chain extra blocks while being written.
            var stream = MemoryManager.Default.GetContextStream(length);

            // Encoding straight into the stream, rather than into a rented buffer handed to
            // GetContextStream(buffer, offset, count): that overload copies the buffer into the
            // stream's own blocks, so staging the bytes first would write them twice.
            stream.WriteString(value, encoding);

            // Hand back a stream ready to be read, not one parked at the end of what was just
            // written. Keep this in step with ToMemoryAsync.
            stream.Position = 0L;

            return stream;
        }

        public static string[] Split(this string expression, string delimiter, string qualifier)
        {
            return Split(expression, delimiter, qualifier, true);
        }

        public static string[] Split(this string expression, string delimiter)
        {
            return Split(expression, delimiter, "\"", true);
        }

        public static string[] Split(this string expression, string delimiter, string qualifier, bool ignoreCase)
        {
            qualifier ??= "\"";
            string statement = String.Format("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))", Regex.Escape(delimiter), Regex.Escape(qualifier));
            //\s+(?=(?:[^"]*"[^"]*")*(?![^"]*"))

            var options = RegexOptions.Multiline; //RegexOptions.Compiled | RegexOptions.Multiline
            if (ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }
            return new Regex(statement, options).Split(expression);
        }
    }
}
