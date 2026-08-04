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
        private readonly string _basePath;

        public FileSystemBlobDataStore(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            Directory.CreateDirectory(_basePath);
        }

        public Task<Stream> CreateAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureWriteLock(blobRecord);
            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> WriteAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureWriteLock(blobRecord);
            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Truncate, FileAccess.Write, FileShare.Read, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureReadLock(blobRecord);
            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> AppendAsync(BlobRecord blobRecord, long offset, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureWriteLock(blobRecord);
            var path = BuildPath(blobRecord);
            Stream file = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read, 81920, true);
            file.Seek(offset, SeekOrigin.Begin);
            return Task.FromResult(file);
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

        public Task<bool> ExistsAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(blobRecord, nameof(blobRecord));
            var location = BuildPath(blobRecord);
            if (!File.Exists(location))
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
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

        private string BuildPath(BlobRecord blobRecord)
        {
            var key = blobRecord.Key;

            var segments = (key ?? string.Empty).Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length > 1)
            {
                // Key carries an explicit path: preceding segments become subfolders,
                // the last segment is the file name.
                var parts = new string[segments.Length + 1];
                parts[0] = _basePath;
                for (var i = 0; i < segments.Length; i++)
                {
                    parts[i + 1] = SanitizeFileName(segments[i]);
                }
                return Path.Combine(parts);
            }

            // Flat key: derive two subfolders from the first 4 hex chars (2 + 2) of its hash
            // to avoid piling every blob into a single directory.
            var fileName = SanitizeFileName(key);
            var hashCode = ComputeHash(key ?? string.Empty);
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

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "blob";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                var isInvalid = false;
                for (var j = 0; j < invalid.Length; j++)
                {
                    if (invalid[j] == ch)
                    {
                        isInvalid = true;
                        break;
                    }
                }
                sb.Append(isInvalid ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
