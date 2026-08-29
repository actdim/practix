using System;

namespace ActDim.BytePath
{
    /// <summary>
    /// Configuration options for <see cref="FileSystemBlobDataStore"/>.
    /// </summary>
    public class FileSystemBlobDataStoreOptions
    {
        /// <summary>
        /// Gets or sets the base directory path where blob files are stored. Defaults to a local <c>blobs</c> folder.
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// Gets or sets the key prefix supported by this store (e.g. <c>"fs:"</c>). Defaults to empty string (catch-all).
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hierarchy separator character used to split keys into nested directory structures on disk.
        /// Defaults to <c>':'</c>. Set to <c>null</c> to disable hierarchical directories and use uniform hash-sharding for all keys.
        /// </summary>
        public char? HierarchySeparator { get; set; } = ':';
    }
}

