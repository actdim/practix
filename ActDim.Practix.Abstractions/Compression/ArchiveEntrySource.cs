namespace ActDim.Practix.Abstractions.Compression
{
    // ArchiveEntryDataSource
    public class ArchiveEntrySource
    {
        public string FullName { get; init; }

        // public long Size { get; set; }

        public Func<Stream> OpenReadAsync { get; init; }
    }
}