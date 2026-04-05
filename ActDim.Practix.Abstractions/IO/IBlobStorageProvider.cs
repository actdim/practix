namespace ActDim.Practix.Abstractions.IO
{
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// Writes the given stream to storage using the specified key.
        /// Returns the actual number of bytes written.
        /// </summary>
        /// <param name="key">The unique key to identify the blob.</param>
        /// <param name="data">The stream containing the blob data.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of bytes written.</returns>
        Task<long> WriteAsync(string key, Stream data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a stream for reading the blob identified by the given key.
        /// Throws an exception if the blob does not exist.
        /// </summary>
        /// <param name="key">The unique key to identify the blob.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A readable stream of the blob data.</returns>
        Task<Stream> ReadAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the blob associated with the specified key if it exists.
        /// </summary>
        /// <param name="key">The unique key to identify the blob.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a blob exists for the specified key.
        /// </summary>
        /// <param name="key">The unique key to check for existence.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the blob exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}