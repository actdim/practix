using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.Compression
{
    public interface IArchiveInfo
    {
        public string FileName { get; set; }

        public long Size { get; set; }

        public ICollection<IArchiveEntry> Entries { get; set; }
    }
}