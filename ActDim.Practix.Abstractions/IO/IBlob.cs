using System.IO;

namespace ActDim.Practix.Abstractions.IO
{
    // IReadableBlob
    public interface IBlob : IBlobEntry
    {
        /// <summary>
        /// Read
        /// </summary>
        /// <returns></returns>
        public Stream OpenRead();
    }
}
