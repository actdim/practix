using System;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    /// <summary>
    /// Represents the result of a blob acquisition or mutation operation, holding the lock handle and record.
    /// </summary>
    public sealed class BlobResult : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobResult"/> class.
        /// </summary>
        /// <param name="errorCode">The status or error code of the operation.</param>
        /// <param name="record">The blob record handle, if acquired.</param>
        /// <param name="isNew">A value indicating whether the record was newly created.</param>
        public BlobResult(BlobErrorCode errorCode, BlobRecord record = null, bool isNew = false)
        {
            ErrorCode = errorCode;
            Record = record;
            IsNew = isNew;
        }

        /// <summary>
        /// Gets the operation error code.
        /// </summary>
        public BlobErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets the associated blob record handle, or null if the operation failed.
        /// </summary>
        public BlobRecord Record { get; }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded (<see cref="ErrorCode"/> == <see cref="BlobErrorCode.None"/>).
        /// </summary>
        public bool IsSuccess => ErrorCode == BlobErrorCode.None;

        /// <summary>
        /// Gets or sets a value indicating whether the blob record was newly created.
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Deconstructs the result into error code and record.
        /// </summary>
        public void Deconstruct(out BlobErrorCode errorCode, out BlobRecord record)
            => (errorCode, record) = (ErrorCode, Record);

        /// <inheritdoc />
        public void Dispose() => Record?.Dispose();

        /// <inheritdoc />
        public ValueTask DisposeAsync() =>
            Record?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
