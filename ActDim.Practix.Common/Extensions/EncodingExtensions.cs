using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class EncodingExtensions
    {
        /// <summary>
        /// Encode stream bytes to string using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        public static string GetString(this Encoding encoding, Stream stream)
        {
            return stream.GetString(encoding);
        }

        // TODO: GetStringAsync

        public static Task<MemoryStream> GetStreamAsync(this Encoding encoding, string value, CancellationToken ct)
        {
            return value.ToMemoryAsync(encoding, ct);
        }

        // TODO: (Copy)ToStreamAsync
    }
}
