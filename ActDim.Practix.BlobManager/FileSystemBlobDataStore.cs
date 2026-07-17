using System;
using System.IO;
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
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(_basePath);
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> WriteAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Truncate, FileAccess.Write, FileShare.Read, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            EnsureReadLock(blobRecord);

            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<Stream> AppendAsync(BlobRecord blobRecord, long offset, CancellationToken ct)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Stream file = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read, 81920, true);
            file.Seek(offset, SeekOrigin.Begin);
            return Task.FromResult(file);
        }

        public Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            if (blobRecord == null)
                throw new ArgumentNullException(nameof(blobRecord));

            var location = BuildPath(blobRecord);
            if (!File.Exists(location))
            {
                return Task.FromResult((string)null);
            }
            return Task.FromResult(location);
        }

        public Task<bool> ExistsAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            if (blobRecord == null)
                throw new ArgumentNullException(nameof(blobRecord));
            var location = BuildPath(blobRecord);
            if (!File.Exists(location))
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        private static void EnsureReadLock(BlobRecord blobRecord)
        {
            if (blobRecord == null)
                throw new ArgumentNullException(nameof(blobRecord));

            if (blobRecord.LockType != LockType.Read && blobRecord.LockType != LockType.Write)
                throw new InvalidOperationException("Read requires a read or write lock on the blob record.");
        }

        private static void EnsureWriteLock(BlobRecord blobRecord)
        {
            if (blobRecord == null)
                throw new ArgumentNullException(nameof(blobRecord));

            if (blobRecord.LockType != LockType.Write)
                throw new InvalidOperationException("Write requires a write lock on the blob record.");
        }

        private string BuildPath(BlobRecord blobRecord)
        {
            var fileName = SanitizeFileName(blobRecord.Key);
            return Path.Combine(_basePath, fileName);
        }

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "blob";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                var isInvalid = false;
                for (var j = 0; j < invalid.Length; j++)
                {
                    if (invalid[j] == ch) { isInvalid = true; break; }
                }
                sb.Append(isInvalid ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
