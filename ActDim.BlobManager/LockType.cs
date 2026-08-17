namespace ActDim.BlobManager
{
    /// <summary>
    /// Specifies the lock concurrency mode held on a blob record.
    /// </summary>
    public enum LockType
    {
        /// <summary>
        /// No lock is held.
        /// </summary>
        None = 0,

        /// <summary>
        /// Shared read lock is held.
        /// </summary>
        Read = 1,

        /// <summary>
        /// Exclusive write lock is held.
        /// </summary>
        Write = 2
    }
}
