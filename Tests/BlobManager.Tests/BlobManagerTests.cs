using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.BlobManager.Tests
{
    public class BlobManagerTests
    {
        [Fact]
        public async Task GetForReadingAsync_UpdatesAccessedAtAndExpiresAt_OnDisposeAsync()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();
            var options = new TestBlobStoreOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(30)
            };

            var (_, created) = await env.Manager.TryGetOrSetAsync("read-key", options, LockType.Read, ct);
            await using (created) { }

            DateTimeOffset accessedBefore;
            DateTimeOffset? expiresBefore;
            var (_, first) = await env.Manager.TryGetForReadingAsync("read-key", ct);
            await using (first)
            {
                accessedBefore = first.AccessedAt;
                expiresBefore = first.ExpiresAt;
                await Task.Delay(1100, ct);
            }

            DateTimeOffset accessedAfter;
            DateTimeOffset? expiresAfter;
            var (_, second) = await env.Manager.TryGetForReadingAsync("read-key", ct);
            await using (second)
            {
                accessedAfter = second.AccessedAt;
                expiresAfter = second.ExpiresAt;
            }

            Assert.True(accessedAfter > accessedBefore);
            Assert.True(expiresBefore.HasValue);
            Assert.True(expiresAfter.HasValue);
            Assert.True(expiresAfter > expiresBefore);
        }

        [Fact]
        public async Task GetForWritingAsync_UpdatesUpdatedAtAndAccessedAt_OnDisposeAsync()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, created) = await env.Manager.TryGetOrSetAsync("write-key", ct);
            await using (created) { }

            DateTimeOffset accessedBefore;
            DateTimeOffset updatedBefore;
            var (_, first) = await env.Manager.TryGetForWritingAsync("write-key", ct);
            await using (first)
            {
                accessedBefore = first.AccessedAt;
                updatedBefore = first.UpdatedAt;
                await Task.Delay(1100, ct);
            }

            DateTimeOffset accessedAfter;
            DateTimeOffset updatedAfter;
            var (_, second) = await env.Manager.TryGetForWritingAsync("write-key", ct);
            await using (second)
            {
                accessedAfter = second.AccessedAt;
                updatedAfter = second.UpdatedAt;
            }

            Assert.True(accessedAfter > accessedBefore);
            Assert.True(updatedAfter > updatedBefore);
        }

        [Fact]
        public async Task ReadLock_BlocksWrite_UntilDisposed()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment(TimeSpan.FromMilliseconds(200));

            var (_, setup) = await env.Manager.TryGetOrSetAsync("lock-key", ct);
            await using (setup) { }

            var (readEc, read) = await env.Manager.TryGetForReadingAsync("lock-key", ct);
            Assert.Equal(BlobErrorCode.None, readEc);
            await using (read)
            {
                var (ec, _) = await env.Manager.TryGetForWritingAsync("lock-key", ct);
                Assert.Equal(BlobErrorCode.Timeout, ec);
            }

            var (writeEc, write) = await env.Manager.TryGetForWritingAsync("lock-key", ct);
            Assert.Equal(BlobErrorCode.None, writeEc);
            await using (write)
            {
                Assert.NotNull(write);
            }
        }

        [Fact]
        public async Task DataStore_WriteRead_RoundTrip()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("data-key", ct);
            await using (record)
            {
                record.Metadata = "data.txt";

                await using (var writeStream = await env.Manager.DataStore.WriteAsync(record, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("hello-blob");
                    await writeStream.WriteAsync(payload, 0, payload.Length, ct);
                }
            }

            var (_, readRecord) = await env.Manager.TryGetForReadingAsync("data-key", ct);
            await using (readRecord)
            {
                await using var readStream = await env.Manager.DataStore.ReadAsync(readRecord, ct);
                using var reader = new StreamReader(readStream, Encoding.UTF8, false, 1024, false);
                var text = await reader.ReadToEndAsync(ct);
                Assert.Equal("hello-blob", text);
            }
        }

        [Fact]
        public async Task DataStore_ResolveLocation_ReturnsPath()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("loc-key", ct);
            await using (record)
            {
                record.Metadata = "photo.png";
                var location = await env.Manager.DataStore.ResolveLocationAsync(record, ct);
                Assert.False(string.IsNullOrWhiteSpace(location));
                Assert.EndsWith(".png", location, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task DataStore_Read_RequiresLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("no-lock-read", ct);
            await using (setup) { }

            var record = new BlobRecord
            {
                Key = "no-lock-read",
                Metadata = "file.txt",
                LockType = LockType.None
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await env.Manager.DataStore.ReadAsync(record, ct);
            });
        }

        [Fact]
        public async Task DataStore_Write_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("no-lock-write", ct);
            await using (setup) { }

            var record = new BlobRecord
            {
                Key = "no-lock-write",
                Metadata = "file.txt",
                LockType = LockType.Read
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await env.Manager.DataStore.WriteAsync(record, ct);
            });
        }

        private sealed class TestBlobStoreOptions : IBlobStoreOptions
        {
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? Ttl { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public string ContentType { get; set; }
            public string Hash { get; set; }
            public string Metadata { get; set; }
        }

        private sealed class TestEnvironment : IAsyncDisposable
        {
            private readonly string _dbPath;
            private readonly string _dataPath;

            public BlobManager Manager { get; }

            public TestEnvironment()
                : this(TimeSpan.FromSeconds(2))
            {
            }

            public TestEnvironment(TimeSpan defaultTimeout)
            {
                _dbPath = Path.Combine(Path.GetTempPath(), "blob_manager_" + Guid.NewGuid().ToString("N") + ".db");
                _dataPath = Path.Combine(Path.GetTempPath(), "blob_manager_files_" + Guid.NewGuid().ToString("N"));

                var registry = new SQLiteBlobRegistry(_dbPath, defaultTimeout);
                var dataStore = new FileSystemBlobDataStore(_dataPath);
                Manager = new BlobManager(dataStore, registry);
            }

            public ValueTask DisposeAsync()
            {
                try
                {
                    if (File.Exists(_dbPath))
                        File.Delete(_dbPath);
                }
                catch { }

                try
                {
                    if (Directory.Exists(_dataPath))
                        Directory.Delete(_dataPath, true);
                }
                catch { }

                return ValueTask.CompletedTask;
            }
        }
    }
}
