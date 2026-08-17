using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    /// <summary>
    /// Turns a write-only producer into a readable stream, so a store that can only consume content
    /// still supports push-style writing. This is the default behind
    /// <see cref="IBlobDataStore.PutAsync(BlobRecord, Func{Stream, CancellationToken, Task}, CancellationToken)"/>;
    /// a store that can hand out its own destination stream — as
    /// a file-system store does — overrides that and never comes here.
    /// </summary>
    internal static class ProducerStreamBridge
    {
        public static Task<long> PutAsync(
            IBlobDataStore dataStore,
            BlobRecord blobRecord,
            Func<Stream, CancellationToken, Task> produce,
            CancellationToken ct)
        {
            return ProduceIntoAsync(dataStore, blobRecord, produce, append: false, ct);
        }

        public static Task<long> AppendAsync(
            IBlobDataStore dataStore,
            BlobRecord blobRecord,
            Func<Stream, CancellationToken, Task> produce,
            CancellationToken ct)
        {
            return ProduceIntoAsync(dataStore, blobRecord, produce, append: true, ct);
        }

        private static async Task<long> ProduceIntoAsync(
            IBlobDataStore dataStore,
            BlobRecord blobRecord,
            Func<Stream, CancellationToken, Task> produce,
            bool append,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(dataStore, nameof(dataStore));
            ArgumentNullException.ThrowIfNull(produce, nameof(produce));

            var pipe = new Pipe();
            var producing = ProduceAsync(pipe.Writer, produce, ct);

            try
            {
                await using var source = pipe.Reader.AsStream();

                var size = append
                    ? await dataStore.AppendAsync(blobRecord, source, ct)
                    : await dataStore.PutAsync(blobRecord, source, ct);

                // Reaching here means the store read to the end of the pipe, which only happens once
                // the writer has completed вЂ” so the producer has finished. Awaiting it is a formality
                // that keeps the task from being abandoned.
                await producing;
                return size;
            }
            catch
            {
                // The store failed, so the producer may be parked writing into a full pipe.
                // Completing the reader releases it; its own outcome is observed so it cannot
                // resurface as an unhandled task exception, and the store's failure is the one
                // reported to the caller.
                await pipe.Reader.CompleteAsync();
                await producing;
                throw;
            }
        }

        private static async Task ProduceAsync(
            PipeWriter writer,
            Func<Stream, CancellationToken, Task> produce,
            CancellationToken ct)
        {
            Exception failure = null;
            try
            {
                // leaveOpen so that completing the writer stays this method's decision: a producer
                // that disposes the stream it was given must not decide how the pipe ends.
                await produce(writer.AsStream(leaveOpen: true), ct);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            // A producer failure travels through the pipe: the reader rethrows it, the store's read
            // fails, and the caller sees the original exception from the write call. So this method
            // never throws on its own вЂ” there is exactly one path out.
            await writer.CompleteAsync(failure);
        }
    }
}
