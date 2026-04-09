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

        public Stream Append(BlobRecord blobRecord, Stream stream, long offset)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(_basePath);

            var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            file.Seek(offset, SeekOrigin.Begin);
            if (stream != null)
            {
                stream.CopyTo(file);
                file.Flush();
            }

            return file;
        }

        public async Task<Stream> AppendAsync(BlobRecord blobRecord, Stream stream, long offset, CancellationToken ct)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(_basePath);

            var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 81920, true);
            file.Seek(offset, SeekOrigin.Begin);
            if (stream != null)
            {
                await stream.CopyToAsync(file, 81920, ct);
                await file.FlushAsync(ct);
            }

            return file;
        }

        public Stream Read(BlobRecord blobRecord)
        {
            EnsureReadLock(blobRecord);

            var path = BuildPath(blobRecord);
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            EnsureReadLock(blobRecord);

            var path = BuildPath(blobRecord);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true);
            return Task.FromResult(stream);
        }

        public Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            if (blobRecord == null)
            {
                throw new ArgumentNullException(nameof(blobRecord));
            }

            return Task.FromResult(BuildPath(blobRecord));
        }

        public Stream Write(BlobRecord blobRecord)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(_basePath);
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public Task<Stream> WriteAsync(BlobRecord blobRecord, CancellationToken ct)
        {
            EnsureWriteLock(blobRecord);

            var path = BuildPath(blobRecord);
            Directory.CreateDirectory(_basePath);
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
            return Task.FromResult(stream);
        }

        private static void EnsureReadLock(BlobRecord blobRecord)
        {
            if (blobRecord == null)
            {
                throw new ArgumentNullException(nameof(blobRecord));
            }

            if (blobRecord.LockType != LockType.Read && blobRecord.LockType != LockType.Write)
            {
                throw new InvalidOperationException("Read requires a read or write lock on the blob record.");
            }
        }

        private static void EnsureWriteLock(BlobRecord blobRecord)
        {
            if (blobRecord == null)
            {
                throw new ArgumentNullException(nameof(blobRecord));
            }

            if (blobRecord.LockType != LockType.Write)
            {
                throw new InvalidOperationException("Write requires a write lock on the blob record.");
            }
        }

        private string BuildPath(BlobRecord blobRecord)
        {
            var safeKey = SanitizeFileName(blobRecord.Key);
            var extension = Path.GetExtension(blobRecord.Metadata ?? string.Empty);
            return Path.Combine(_basePath, safeKey + extension);
        }

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


