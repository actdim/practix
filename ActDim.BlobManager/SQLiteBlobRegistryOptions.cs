using System;

namespace ActDim.BlobManager
{
    /// <summary>
    /// Configuration options for <see cref="SQLiteBlobRegistry"/>.
    /// </summary>
    public class SQLiteBlobRegistryOptions
    {
        /// <summary>
        /// Gets or sets the SQLite database file name or full path. Defaults to <c>registry.db</c>.
        /// </summary>
        public string DatabaseName { get; set; } = "registry.db";

        /// <summary>
        /// Gets or sets the directory containing the SQLite database file. Defaults to current directory or base storage directory.
        /// </summary>
        public string BaseDirectory { get; set; }
    }
}
