using System;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public class FileSystemBlobDataStore : IBlobDataStore
    {
        private const int BufferSize = 81920;

        private const char EscapeChar = '%';

        private readonly string _basePath;

        public FileSystemBlobDataStore(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            Directory.CreateDirectory(_basePath);
        }

        public Task<long> WriteAsync(BlobRecord blobRecord, Stream content, CancellationToken ct)
        {
            return CopyIntoAsync(blobRecord, content, FileMode.Create, ct);
        }

        public Task<long> AppendAsync(BlobRecord blobRecord, Stream content, CancellationToken ct)
        {
            // FileMode.Append creates the file when absent and positions at its end, so the current
            // size never has to be looked up.
            return CopyIntoAsync(blobRecord, content, FileMode.Append, ct);
        }

        public Task<long> WriteAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
        {
            return ProduceIntoAsync(blobRecord, produce, FileMode.Create, ct);
        }

        public Task<long> AppendAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
        {
            return ProduceIntoAsync(blobRecord, produce, FileMode.Append, ct);
        }

        private Task<long> CopyIntoAsync(BlobRecord blobRecord, Stream content, FileMode mode, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            return WriteThroughAsync(blobRecord, (file, token) => content.CopyToAsync(file, BufferSize, token), mode, ct);
        }

        private Task<long> ProduceIntoAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, FileMode mode, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(produce, nameof(produce));
            // The producer gets the destination file itself; the interface default would only route
            // the same bytes through a pipe.
            return WriteThroughAsync(blobRecord, produce, mode, ct);
        }

        private async Task<long> WriteThroughAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> write, FileMode mode, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureWriteLock(blobRecord);

            var path = EnsureDirectory(blobRecord);
            await using var file = new FileStream(path, mode, FileAccess.Write, FileShare.Read, BufferSize, true);

            await write(file, ct);
            await file.FlushAsync(ct);

            // Length rather than Position: a producer given this stream directly may seek within it,
            // in which case the position is not the end. Recorded here because this is the moment the
            // size is known for certain, without asking the file system again.
            var size = file.Length;
            blobRecord.Size = size;
            return size;
        }

        public Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureReadLock(blobRecord);
            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, true);
            return Task.FromResult(stream);
        }

        public Task<bool> DeleteAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureWriteLock(blobRecord);
            var path = BuildPath(blobRecord);
            if (!File.Exists(path))
            {
                return Task.FromResult(false);
            }

            File.Delete(path);
            PruneEmptyDirectories(Path.GetDirectoryName(path));
            return Task.FromResult(true);
        }

        public Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(blobRecord, nameof(blobRecord));
            var location = BuildPath(blobRecord);
            if (!File.Exists(location))
            {
                return Task.FromResult((string)null);
            }
            return Task.FromResult(location);
        }

        public Task<long?> GetSizeAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(blobRecord, nameof(blobRecord));
            var info = new FileInfo(BuildPath(blobRecord));
            if (!info.Exists)
            {
                return Task.FromResult((long?)null);
            }
            return Task.FromResult((long?)info.Length);
        }

        private static void EnsureReadLock(BlobRecord blobRecord)
        {
            ArgumentNullException.ThrowIfNull(blobRecord, nameof(blobRecord));

            if (blobRecord.LockType != LockType.Read && blobRecord.LockType != LockType.Write)
            {
                throw new InvalidOperationException("Read requires a read or write lock on the blob record.");
            }
        }

        private static void EnsureWriteLock(BlobRecord blobRecord)
        {
            ArgumentNullException.ThrowIfNull(blobRecord, nameof(blobRecord));

            if (blobRecord.LockType != LockType.Write)
            {
                throw new InvalidOperationException("Write requires a write lock on the blob record.");
            }
        }

        /// <summary>
        /// Resolves the path and makes sure its shard directories exist, for the operations that
        /// may have to create the content.
        /// </summary>
        private string EnsureDirectory(BlobRecord blobRecord)
        {
            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        private string BuildPath(BlobRecord blobRecord)
        {
            var key = blobRecord.Key ?? string.Empty;

            // Only '/' separates. A backslash is an ordinary character that gets escaped, so 'a\b' stays
            // a distinct key rather than aliasing onto 'a/b'.
            var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length > 1)
            {
                // Key carries an explicit path: preceding segments become subfolders,
                // the last segment is the file name.
                var parts = new string[segments.Length + 1];
                parts[0] = _basePath;
                for (var i = 0; i < segments.Length; i++)
                {
                    parts[i + 1] = EscapeFileName(segments[i]);
                }
                return Path.Combine(parts);
            }

            // Flat key: derive two subfolders from the first 4 hex chars (2 + 2) of its hash
            // to avoid piling every blob into a single directory.
            var fileName = EscapeFileName(key);
            var hashCode = ComputeHash(key);
            return Path.Combine(_basePath, hashCode[..2], hashCode[2..4], fileName);
        }

        private static string ComputeHash(byte[] bytes)
        {
            // XxHash3 is a fast, non-cryptographic hash suitable for cache keys, unique identifiers,
            // and detecting accidental data changes in trusted environments.
            // Advantages:
            // - Significantly faster than MD5 and SHA-256.
            // - Produces a compact 64-bit hash (16 hex characters), shorter than MD5 (32), SHA1 (40) and SHA-256 (64).
            // - Hex encoding (X16) uses only [0-9A-F], making it safe for file names, URLs, and cache keys,
            //   unlike Base64 which may contain '+', '/', and '=' characters.
            var hash = XxHash3.HashToUInt64(bytes);
            return hash.ToString("X16");
        }

        private static string ComputeHash(string value)
        {
            return ComputeHash(Encoding.UTF8.GetBytes(value));
        }

        // /// <summary>
        // /// Fast hash computation, but not cryptographic: no protection against intentional tampering,
        // /// but good enough for detecting accidental data corruption or as a fast hash for deduplication/caching
        // /// </summary>
        // /// <param name="path"></param>
        // /// <param name="ct"></param>
        // /// <returns></returns>
        // public static async Task<byte[]> ComputeXxHash3Async(string path, CancellationToken ct)
        // {
        //     await using var stream = new FileStream(
        //         path, FileMode.Open, FileAccess.Read, FileShare.Read,
        //         bufferSize: 1 << 20, // 1 MB
        //         options: FileOptions.SequentialScan | FileOptions.Asynchronous);
        //     var hasher = new XxHash3();
        //     var buffer = ArrayPool<byte>.Shared.Rent(1 << 20);
        //     try
        //     {
        //         int read;
        //         while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        //         {
        //             hasher.Append(buffer.AsSpan(0, read));
        //         }
        //         return hasher.GetCurrentHash();
        //     }
        //     finally
        //     {
        //         ArrayPool<byte>.Shared.Return(buffer);
        //     }
        // }        

        /// <summary>
        /// Removes the directories a deleted blob left empty. Keys are sharded into subfolders,
        /// so without this every deleted blob would leave its shard behind for good.
        /// </summary>
        private void PruneEmptyDirectories(string directory)
        {
            var root = Path.GetFullPath(_basePath);
            var current = directory;

            while (!string.IsNullOrEmpty(current))
            {
                var full = Path.GetFullPath(current);
                if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
                    || !full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (Directory.GetFileSystemEntries(full).Length > 0)
                {
                    return;
                }

                try
                {
                    Directory.Delete(full);
                }
                catch (IOException)
                {
                    // Someone repopulated the directory between the check and the delete.
                    return;
                }

                current = Path.GetDirectoryName(full);
            }
        }

        /// <summary>
        /// Turns a key segment into a file name **reversibly**: anything a file name cannot carry becomes
        /// %XX, and '%' itself is escaped so the mapping stays injective. Two different segments therefore
        /// cannot produce the same name — a lossy replacement would fold ':' and '_' onto one file.
        /// </summary>
        private static string EscapeFileName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "blob";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input.Length);

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                // Windows silently drops a trailing dot or space, which would alias "a." onto "a".
                var isTrimmedAway = i == input.Length - 1 && (ch == '.' || ch == ' ');

                if (ch == EscapeChar || isTrimmedAway || Array.IndexOf(invalid, ch) >= 0)
                {
                    sb.Append(EscapeChar).Append(((int)ch).ToString("X2"));
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
    }
}
