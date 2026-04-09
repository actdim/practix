using Microsoft.IO;

namespace ActDim.Practix.Memory
{
    public class MemoryManager
    {
        public static readonly RecyclableMemoryStreamManager Default;
#pragma warning disable S3963 // "static" fields should be initialized inline
        static MemoryManager()
        {
            Default = new RecyclableMemoryStreamManager();

            var blockSize = 8192; // 8 KB
            var largeBufferMultiple = 1024 * 1024; // 1 MB
            var maxBufferSize = 16 * largeBufferMultiple;
            var maximumFreeSmallPoolBytes = 64 * largeBufferMultiple;
            var maximumFreeLargePoolBytes = 256 * largeBufferMultiple; // 512
            var maximumStreamCapacity = 1024 * largeBufferMultiple;

            Default = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options(
                    blockSize, largeBufferMultiple, maxBufferSize, maximumFreeSmallPoolBytes, maximumFreeLargePoolBytes
                )
            {
                // GenerateCallStacks = true,
                AggressiveBufferReturn = true,
                ThrowExceptionOnToArray = true,
                UseExponentialLargeBuffer = true,
                MaximumStreamCapacity = maximumStreamCapacity
            });

            /*
            Default.StreamCreated += (_, args) =>
            {
                Console.WriteLine($"Stream created: {args.Id}");
            };
            Default.StreamDisposed += (_, args) =>
            {
                Console.WriteLine($"Stream disposed: {args.Id}");
            };
            Default.StreamFinalized += (_, args) =>
            {
                Console.WriteLine($"Stream finalized without dispose: {args.Id}");
            };
            */
        }
#pragma warning restore S3963 // "static" fields should be initialized inline
    }
}
