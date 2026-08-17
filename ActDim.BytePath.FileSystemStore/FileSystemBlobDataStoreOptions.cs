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
    }
}
