using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActDim.BytePath;
using Xunit;

namespace ActDim.BytePath.Tests
{
    public class FileSystemBlobDataStoreTests
    {
        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenFileDoesNotExist()
        {
            var ct = TestContext.Current.CancellationToken;
            var tempDir = Path.Combine(Path.GetTempPath(), "BytePathTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var store = new FileSystemBlobDataStore(tempDir);
                var record = new BlobRecord
                {
                    Key = "non_existent_key",
                    LockType = LockType.Write
                };

                var result = await store.DeleteAsync(record, ct);
                Assert.False(result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenDirectoryDoesNotExist()
        {
            var ct = TestContext.Current.CancellationToken;
            var tempDir = Path.Combine(Path.GetTempPath(), "BytePathTests_" + Guid.NewGuid().ToString("N"));
            // Do not create directory on disk
            var store = new FileSystemBlobDataStore(tempDir);
            var record = new BlobRecord
            {
                Key = "non_existent_dir_key",
                LockType = LockType.Write
            };

            var result = await store.DeleteAsync(record, ct);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_AndPrunesEmptyDirectories_WhenFileExists()
        {
            var ct = TestContext.Current.CancellationToken;
            var tempDir = Path.Combine(Path.GetTempPath(), "BytePathTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var store = new FileSystemBlobDataStore(tempDir);
                var record = new BlobRecord
                {
                    Key = "folder/subfolder/file.dat",
                    LockType = LockType.Write
                };

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test Payload"));
                await store.PutAsync(record, stream, ct);

                var location = await store.ResolveLocationAsync(record, ct);
                Assert.NotNull(location);
                Assert.True(File.Exists(location));

                var result = await store.DeleteAsync(record, ct);
                Assert.True(result);
                Assert.False(File.Exists(location));

                // Verify parent directories were pruned up to base directory
                var parentDir = Path.GetDirectoryName(location);
                if (parentDir != null && !string.Equals(Path.GetFullPath(parentDir), Path.GetFullPath(tempDir), StringComparison.OrdinalIgnoreCase))
                {
                    Assert.False(Directory.Exists(parentDir));
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
