using System;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    /// <summary>
    /// File-system based implementation of <see cref="IBlobDataStore"/> that stores blob contents in sharded directory structures.
    /// </summary>
    public class FileSystemBlobDataStore : IBlobDataStore
    {
        private const int BufferSize = 81920;
        private const char EscapeChar = '%';

        private readonly string _basePath;
        private readonly char? _hierarchySeparator;

        /// <inheritdoc />
        public string KeyPrefix { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemBlobDataStore"/> class with the specified base directory path, key prefix, and hierarchy separator.
        /// </summary>
        /// <param name="basePath">The root directory path where blobs are stored.</param>
        /// <param name="keyPrefix">The key prefix handled by this store (e.g. <c>"fs:"</c>). Defaults to empty string (catch-all).</param>
        /// <param name="hierarchySeparator">The hierarchy separator character used to split keys into subdirectories. Defaults to <c>':'</c>. Set to <c>null</c> for uniform hash-sharding.</param>
        public FileSystemBlobDataStore(string basePath, string keyPrefix = null, char? hierarchySeparator = ':')
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            KeyPrefix = keyPrefix ?? string.Empty;
            _hierarchySeparator = hierarchySeparator;
            Directory.CreateDirectory(_basePath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemBlobDataStore"/> class using the specified options.
        /// </summary>
        /// <param name="options">The storage configuration options.</param>
        public FileSystemBlobDataStore(FileSystemBlobDataStoreOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _basePath = options.BaseDirectory ?? throw new ArgumentNullException(nameof(options.BaseDirectory));
            KeyPrefix = options.KeyPrefix ?? string.Empty;
            _hierarchySeparator = options.HierarchySeparator;
            Directory.CreateDirectory(_basePath);
        }

        /// <inheritdoc />
        public Task<long> PutAsync(BlobRecord blobRecord, Stream content, CancellationToken ct)
        {
            return CopyIntoAsync(blobRecord, content, FileMode.Create, ct);
        }

        /// <inheritdoc />
        public Task<long> AppendAsync(BlobRecord blobRecord, Stream content, CancellationToken ct)
        {
            // FileMode.Append creates the file when absent and positions at its end, so the current
            // size never has to be looked up.
            return CopyIntoAsync(blobRecord, content, FileMode.Append, ct);
        }

        /// <inheritdoc />
        public Task<long> PutAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
        {
            return ProduceIntoAsync(blobRecord, produce, FileMode.Create, ct);
        }

        /// <inheritdoc />
        public Task<long> AppendAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
        {
            return ProduceIntoAsync(blobRecord, produce, FileMode.Append, ct);
        }

        /// <inheritdoc />
        public Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureReadLock(blobRecord);
            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, true);
            return Task.FromResult(stream);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        private string EnsureDirectory(BlobRecord blobRecord)
        {
            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        private string BuildPath(BlobRecord blobRecord)
        {
            var key = blobRecord.Key ?? string.Empty;

            if (!string.IsNullOrEmpty(KeyPrefix) && key.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = key[KeyPrefix.Length..];
            }

            if (_hierarchySeparator.HasValue)
            {
                var segments = key.Split(_hierarchySeparator.Value, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length > 1)
                {
                    var parts = new string[segments.Length + 1];
                    parts[0] = _basePath;
                    for (var i = 0; i < segments.Length; i++)
                    {
                        parts[i + 1] = EscapeFileName(segments[i]);
                    }

                    return Path.Combine(parts);
                }
            }

            var fileName = EscapeFileName(key);
            var hashCode = ComputeHash(key);
            return Path.Combine(_basePath, hashCode[..2], hashCode[2..4], fileName);
        }

        private static string ComputeHash(byte[] bytes)
        {
            var hash = XxHash3.HashToUInt64(bytes);
            return hash.ToString("X16");
        }

        private static string ComputeHash(string value)
        {
            return ComputeHash(Encoding.UTF8.GetBytes(value));
        }

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
                    return;
                }

                current = Path.GetDirectoryName(full);
            }
        }

        private static bool IsWindowsReservedName(ReadOnlySpan<char> name)
        {
            var dotIndex = name.IndexOf('.');
            var stem = dotIndex >= 0 ? name[..dotIndex] : name;

            if (stem.Length == 3)
            {
                if (stem.Equals("CON", StringComparison.OrdinalIgnoreCase)
                    || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                    || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                    || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (stem.Length == 4)
            {
                if ((stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                    && stem[3] >= '1' && stem[3] <= '9')
                {
                    return true;
                }
            }

            return false;
        }

        private static string EscapeFileName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "blob";
            }

            var isReservedDeviceName = IsWindowsReservedName(input.AsSpan());
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input.Length + 4);

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                var isTrimmedAway = i == input.Length - 1 && (ch == '.' || ch == ' ');
                var isFirstCharOfReservedDevice = i == 0 && isReservedDeviceName;

                if (isFirstCharOfReservedDevice || ch == EscapeChar || isTrimmedAway || Array.IndexOf(invalid, ch) >= 0)
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
