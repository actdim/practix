using System;

namespace ActDim.BytePath
{
    /// <summary>
    /// Extension methods for validating and manipulating <see cref="BlobResult"/> instances.
    /// </summary>
    public static class BlobResultExtensions
    {
        /// <summary>
        /// Ensures that the specified <see cref="BlobResult"/> is not null, succeeded, and contains a valid <see cref="BlobRecord"/>.
        /// </summary>
        /// <param name="blobResult">The BLOB operation result to validate.</param>
        /// <returns>The validated <see cref="BlobResult"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobResult"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the BLOB operation failed or the record is missing.</exception>
        public static BlobResult EnsureSuccess(this BlobResult blobResult)
        {
            ArgumentNullException.ThrowIfNull(blobResult);

            if (!blobResult.IsSuccess)
            {
                var keySuffix = blobResult.Record != null ? $" for key '{blobResult.Record.Key}'" : string.Empty;
                throw new InvalidOperationException($"BLOB operation failed{keySuffix}. Error code: {blobResult.ErrorCode}");
            }

            if (blobResult.Record == null)
            {
                throw new InvalidOperationException("BLOB operation reported success but the record is missing.");
            }

            return blobResult;
        }

        /// <summary>
        /// Ensures that the specified <see cref="BlobResult"/> succeeded and returns its non-null <see cref="BlobRecord"/>.
        /// </summary>
        /// <param name="blobResult">The BLOB operation result to validate.</param>
        /// <returns>The non-null <see cref="BlobRecord"/> contained within the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobResult"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the BLOB operation failed or the record is missing.</exception>
        public static BlobRecord EnsureRecord(this BlobResult blobResult)
        {
            return blobResult.EnsureSuccess().Record;
        }
    }
}
