using System.IO;
using System.Text;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class EncodingExtensions
    {
        /// <summary>
        /// Encode stream bytes to string using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        public static string GetString(this Encoding encoding, Stream stream)
        {
            return stream.ToString(encoding);
        }

        // TODO: GetStringAsync
    }
}
