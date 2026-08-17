using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Encoding"/> operations.
    /// </summary>
    public static class EncodingExtensions
    {
        /// <summary>
        /// Decodes bytes from a <see cref="Stream"/> into a string using the specified encoding.
        /// </summary>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="stream">The source byte stream.</param>
        /// <returns>The decoded string.</returns>
        public static string GetString(this Encoding encoding, Stream stream)
        {
            return stream.GetString(encoding);
        }

        /// <summary>
        /// Encodes a string value asynchronously into a pooled <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="value">The string value to encode.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A seekable <see cref="MemoryStream"/> positioned at offset 0.</returns>
        public static Task<MemoryStream> GetStreamAsync(this Encoding encoding, string value, CancellationToken ct)
        {
            return value.ToMemoryAsync(encoding, ct);
        }
    }
}
