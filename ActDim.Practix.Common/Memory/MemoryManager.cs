using Microsoft.IO;

namespace ActDim.Practix.Memory
{
    /// <summary>
    /// Holds process-wide default <see cref="RecyclableMemoryStreamManager"/> instance configured for optimal memory pooling.
    /// </summary>
    public class MemoryManager
    {
        /// <summary>
        /// Gets the process-wide shared <see cref="RecyclableMemoryStreamManager"/> instance.
        /// </summary>
        public static readonly RecyclableMemoryStreamManager Default;

#pragma warning disable S3963 // "static" fields should be initialized inline
        static MemoryManager()
        {
            Default = new RecyclableMemoryStreamManager();

            var blockSize = 8192; // 8 KB
            var largeBufferMultiple = 1024 * 1024; // 1 MB
            var maxBufferSize = 16 * largeBufferMultiple;
            var maximumFreeSmallPoolBytes = 64 * largeBufferMultiple;
            var maximumFreeLargePoolBytes = 256 * largeBufferMultiple;
            var maximumStreamCapacity = 1024 * largeBufferMultiple;

            Default = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options(
                    blockSize, largeBufferMultiple, maxBufferSize, maximumFreeSmallPoolBytes, maximumFreeLargePoolBytes
                )
            {
                AggressiveBufferReturn = true,
                ThrowExceptionOnToArray = true,
                UseExponentialLargeBuffer = true,
                MaximumStreamCapacity = maximumStreamCapacity
            });
        }
#pragma warning restore S3963 // "static" fields should be initialized inline
    }
}
