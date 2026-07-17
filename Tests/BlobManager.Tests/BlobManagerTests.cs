using System;
using System.Collections.Generic;
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
            var options = new BlobStoreOptions
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
        public async Task DataStore_CreateRead_RoundTrip()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("data-key", ct);
            await using (record)
            {
                await using (var stream = await env.Manager.DataStore.CreateAsync(record, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("hello-blob");
                    await stream.WriteAsync(payload, 0, payload.Length, ct);
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
        public async Task DataStore_WriteAsync_TruncatesExistingContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("write-trunc-key", ct);
            await using (record)
            {
                await using (var stream = await env.Manager.DataStore.CreateAsync(record, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("original content");
                    await stream.WriteAsync(payload, 0, payload.Length, ct);
                }
            }

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("write-trunc-key", ct);
            await using (writeRecord)
            {
                await using (var stream = await env.Manager.DataStore.WriteAsync(writeRecord, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("new");
                    await stream.WriteAsync(payload, 0, payload.Length, ct);
                }
            }

            var (_, readRecord) = await env.Manager.TryGetForReadingAsync("write-trunc-key", ct);
            await using (readRecord)
            {
                await using var readStream = await env.Manager.DataStore.ReadAsync(readRecord, ct);
                using var reader = new StreamReader(readStream, Encoding.UTF8, false, 1024, false);
                var text = await reader.ReadToEndAsync(ct);
                Assert.Equal("new", text);
            }
        }

        [Fact]
        public async Task DataStore_ResolveLocation_ReturnsPath()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("loc-key.png", ct);
            await using (record)
            {
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
        public async Task DataStore_CreateAsync_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var record = new BlobRecord { Key = "no-lock-create", Metadata = "file.txt", LockType = LockType.Read };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.Manager.DataStore.CreateAsync(record, ct));
        }

        [Fact]
        public async Task DataStore_WriteAsync_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var record = new BlobRecord { Key = "no-lock-write", Metadata = "file.txt", LockType = LockType.Read };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.Manager.DataStore.WriteAsync(record, ct));
        }

        [Fact]
        public async Task DataStore_AppendAsync_AppendsStreamAfterOffset()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("append-key", ct);
            await using (record)
            {
                await using (var stream = await env.Manager.DataStore.CreateAsync(record, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("hello");
                    await stream.WriteAsync(payload, 0, payload.Length, ct);
                }
            }

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("append-key", ct);
            await using (writeRecord)
            {
                await using var appendStream = await env.Manager.DataStore.AppendAsync(writeRecord, 5, ct);
                var patch = Encoding.UTF8.GetBytes("-world");
                await appendStream.WriteAsync(patch, 0, patch.Length, ct);
            }

            var (_, readRecord) = await env.Manager.TryGetForReadingAsync("append-key", ct);
            await using (readRecord)
            {
                await using var readStream = await env.Manager.DataStore.ReadAsync(readRecord, ct);
                using var reader = new StreamReader(readStream, Encoding.UTF8, false, 1024, false);
                var text = await reader.ReadToEndAsync(ct);
                Assert.Equal("hello-world", text);
            }
        }

        [Fact]
        public async Task DataStore_AppendAsync_ReturnsWritableStreamAtOffset()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("append-null-key", ct);
            await using (record)
            {
                await using (var stream = await env.Manager.DataStore.CreateAsync(record, ct))
                {
                    var payload = Encoding.UTF8.GetBytes("hello");
                    await stream.WriteAsync(payload, 0, payload.Length, ct);
                }
            }

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("append-null-key", ct);
            await using (writeRecord)
            {
                await using var appendStream = await env.Manager.DataStore.AppendAsync(writeRecord, 5, ct);
                var patch = Encoding.UTF8.GetBytes(" world");
                await appendStream.WriteAsync(patch, 0, patch.Length, ct);
            }

            var (_, readRecord) = await env.Manager.TryGetForReadingAsync("append-null-key", ct);
            await using (readRecord)
            {
                await using var readStream = await env.Manager.DataStore.ReadAsync(readRecord, ct);
                using var reader = new StreamReader(readStream, Encoding.UTF8, false, 1024, false);
                var text = await reader.ReadToEndAsync(ct);
                Assert.Equal("hello world", text);
            }
        }

        [Fact]
        public async Task DataStore_AppendAsync_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("no-lock-append", ct);
            await using (setup) { }

            var record = new BlobRecord
            {
                Key = "no-lock-append",
                Metadata = "file.txt",
                LockType = LockType.Read
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await env.Manager.DataStore.AppendAsync(record, 0, ct);
            });
        }

        // ── KeyNotFound ──────────────────────────────────────────────────────────

        [Fact]
        public async Task TryGetForReadingAsync_KeyNotFound_ReturnsKeyNotFound()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (ec, record) = await env.Manager.TryGetForReadingAsync("nonexistent", ct);

            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
            Assert.Null(record);
        }

        [Fact]
        public async Task TryGetForWritingAsync_KeyNotFound_ReturnsKeyNotFound()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (ec, record) = await env.Manager.TryGetForWritingAsync("nonexistent", ct);

            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
            Assert.Null(record);
        }

        // ── Lock semantics ───────────────────────────────────────────────────────

        [Fact]
        public async Task WriteLock_BlocksRead_UntilDisposed()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment(TimeSpan.FromMilliseconds(200));

            var (_, setup) = await env.Manager.TryGetOrSetAsync("wlock-key", ct);
            await using (setup) { }

            var (writeEc, write) = await env.Manager.TryGetForWritingAsync("wlock-key", ct);
            Assert.Equal(BlobErrorCode.None, writeEc);
            await using (write)
            {
                var (ec, _) = await env.Manager.TryGetForReadingAsync("wlock-key", ct);
                Assert.Equal(BlobErrorCode.Timeout, ec);
            }

            var (readEc, read) = await env.Manager.TryGetForReadingAsync("wlock-key", ct);
            Assert.Equal(BlobErrorCode.None, readEc);
            await using (read) { }
        }

        [Fact]
        public async Task MultipleReaders_CanHoldReadLocksSimultaneously()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("multi-read-key", ct);
            await using (setup) { }

            var (ec1, read1) = await env.Manager.TryGetForReadingAsync("multi-read-key", ct);
            var (ec2, read2) = await env.Manager.TryGetForReadingAsync("multi-read-key", ct);

            Assert.Equal(BlobErrorCode.None, ec1);
            Assert.Equal(BlobErrorCode.None, ec2);

            await using (read1) { }
            await using (read2) { }
        }

        // ── IsNew ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task TryGetOrSetAsync_NewKey_IsNew_True()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await using var result = await env.Manager.TryGetOrSetAsync("new-key", ct);

            Assert.True(result.IsNew);
        }

        [Fact]
        public async Task TryGetOrSetAsync_ExistingKey_IsNew_False()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, first) = await env.Manager.TryGetOrSetAsync("existing-key", ct);
            await using (first) { }

            await using var result = await env.Manager.TryGetOrSetAsync("existing-key", ct);

            Assert.False(result.IsNew);
        }

        // ── TryGetOrSetAsync with options ────────────────────────────────────────

        [Fact]
        public async Task TryGetOrSetAsync_WithOptions_AppliesContentTypeAndMetadata()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var options = new BlobStoreOptions
            {
                ContentType = "image/png",
                Metadata = "photo.png"
            };

            var (ec, record) = await env.Manager.TryGetOrSetAsync("opts-key", options, LockType.Write, ct);
            await using (record)
            {
                Assert.Equal(BlobErrorCode.None, ec);
                Assert.Equal("image/png", record.ContentType);
                Assert.Equal("photo.png", record.Metadata);
            }
        }

        [Fact]
        public async Task TryGetOrSetAsync_AbsoluteExpiration_SetsExpiresAt()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var expiry = DateTimeOffset.UtcNow.AddHours(1);
            var options = new BlobStoreOptions { AbsoluteExpiration = expiry };

            var (ec, record) = await env.Manager.TryGetOrSetAsync("abs-expiry-key", options, LockType.Write, ct);
            await using (record)
            {
                Assert.Equal(BlobErrorCode.None, ec);
                Assert.NotNull(record.ExpiresAt);
                Assert.True(record.ExpiresAt.Value >= expiry.AddSeconds(-1));
                Assert.True(record.ExpiresAt.Value <= expiry.AddSeconds(1));
            }
        }

        [Fact]
        public async Task TryGetOrSetAsync_ExistingKey_WithReadLockType_ReturnsReadLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("readlock-downgrade-key", ct);
            await using (setup) { }

            var (ec, record) = await env.Manager.TryGetOrSetAsync("readlock-downgrade-key", null, LockType.Read, ct);
            await using (record)
            {
                Assert.Equal(BlobErrorCode.None, ec);
                Assert.Equal(LockType.Read, record.LockType);
            }
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_RemovesRecord()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, setup) = await env.Manager.TryGetOrSetAsync("delete-key", ct);
            await using (setup) { }

            await env.Manager.DeleteAsync("delete-key", ct);

            var (ec, _) = await env.Manager.TryGetForReadingAsync("delete-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
        }

        [Fact]
        public async Task DeleteAsync_KeyNotFound_ThrowsKeyNotFoundException()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await env.Manager.DeleteAsync("nonexistent-delete", ct));
        }

        // ── DeleteExpiredAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteExpiredAsync_RemovesExpiredRecords()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var options = new BlobStoreOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(-2)
            };
            var (_, record) = await env.Manager.TryGetOrSetAsync("expired-key", options, LockType.Write, ct);
            await using (record) { }

            var deleted = await env.Manager.DeleteExpiredAsync(ct);

            Assert.True(deleted > 0);
            var (ec, _) = await env.Manager.TryGetForReadingAsync("expired-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
        }

        [Fact]
        public async Task DeleteExpiredAsync_SkipsNonExpiredRecords()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var options = new BlobStoreOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
            };
            var (_, record) = await env.Manager.TryGetOrSetAsync("fresh-key", options, LockType.Write, ct);
            await using (record) { }

            await env.Manager.DeleteExpiredAsync(ct);

            var (ec, fresh) = await env.Manager.TryGetForReadingAsync("fresh-key", ct);
            Assert.Equal(BlobErrorCode.None, ec);
            await using (fresh) { }
        }

        // ── DeleteOlderThanAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task DeleteOlderThanAsync_RemovesMatchingRecords()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("old-key", ct);
            await using (record) { }

            var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct);

            Assert.True(deleted > 0);
            var (ec, _) = await env.Manager.TryGetForReadingAsync("old-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
        }

        [Fact]
        public async Task DeleteOlderThanAsync_SkipsLockedRecords_ByDefault()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("locked-old-key", ct);
            await using (record)
            {
                var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct);

                Assert.Equal(0, deleted);
            }

            var (ec, _) = await env.Manager.TryGetForReadingAsync("locked-old-key", ct);
            Assert.Equal(BlobErrorCode.None, ec);
        }

        [Fact]
        public async Task DeleteOlderThanAsync_ForceDeleteLocked_RemovesLockedRecords()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("force-old-key", ct);
            await using (record)
            {
                var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct, forceDeleteLocked: true);

                Assert.True(deleted > 0);
            }

            var (ec, _) = await env.Manager.TryGetForReadingAsync("force-old-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
        }

        // ── QueryAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_PatternMatch_ReturnsMatchingKeys()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            foreach (var key in new[] { "qry:a", "qry:b", "other:c" })
            {
                var (_, r) = await env.Manager.TryGetOrSetAsync(key, ct);
                await using (r) { }
            }

            var results = await env.Manager.QueryAsync("qry:*", ct);

            Assert.Equal(2, results.Count);
            Assert.Contains("qry:a", results);
            Assert.Contains("qry:b", results);
            Assert.DoesNotContain("other:c", results);
        }

        [Fact]
        public async Task QueryAsync_NoMatch_ReturnsEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var results = await env.Manager.QueryAsync("nomatch:*", ct);

            Assert.Empty(results);
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
