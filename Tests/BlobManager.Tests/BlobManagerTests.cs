using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.BlobManager.Tests
{
    public class BlobManagerTests
    {
        /// <summary>
        /// <see cref="Encoding.UTF8"/> emits a byte-order mark, which would add three bytes to every
        /// blob written through a <see cref="StreamWriter"/>.
        /// </summary>
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// The data store consumes a stream, so tests supply their payload as one.
        /// </summary>
        private static Stream Content(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        [Fact]
        public async Task GetForReadingAsync_UpdatesAccessedAtAndExpiresAt_OnDisposeAsync()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();
            var options = new BlobStoreOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(30)
            };

            await env.SeedAsync("read-key", ct, options);

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

            await env.SeedAsync("write-key", ct);

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

            await env.SeedAsync("lock-key", ct);

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

            // WriteAsync on a brand-new key creates the content, and reports its size right away.
            var (_, record) = await env.Manager.TryGetOrSetAsync("data-key", ct);
            long written;
            await using (record)
            {
                written = await env.Manager.DataStore.PutAsync(record, Content("hello-blob"), ct);
            }

            Assert.Equal(10, written);
            Assert.Equal("hello-blob", await env.ReadTextAsync("data-key", ct));
        }

        [Fact]
        public async Task DataStore_PutAsync_ReplacesExistingContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("write-trunc-key", ct, content: "original content");

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("write-trunc-key", ct);
            await using (writeRecord)
            {
                Assert.Equal(3, await env.Manager.DataStore.PutAsync(writeRecord, Content("new"), ct));
            }

            Assert.Equal("new", await env.ReadTextAsync("write-trunc-key", ct));
        }

        [Fact]
        public async Task DataStore_ResolveLocation_ReturnsPath()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("loc-key.png", ct);

            var (_, record) = await env.Manager.TryGetForReadingAsync("loc-key.png", ct);
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

            await env.SeedAsync("no-lock-read", ct);

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

        // ── Push-style writing (BlobDataStoreExtensions) ─────────────────────────

        [Fact]
        public async Task DataStore_PutAsync_WithProducer_WritesContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("produce-key", ct);
            long written;
            await using (record)
            {
                // A write-only producer: it is handed a stream instead of supplying one.
                written = await env.Manager.DataStore.PutAsync(record, async (stream, token) =>
                {
                    await using var writer = new StreamWriter(stream, Utf8NoBom, 1024, true);
                    await writer.WriteAsync("produced".AsMemory(), token);
                }, ct);
            }

            Assert.Equal(8, written);
            Assert.Equal("produced", await env.ReadTextAsync("produce-key", ct));
        }

        [Fact]
        public async Task DataStore_AppendAsync_WithProducer_AppendsContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("produce-append-key", ct, content: "head");

            var (_, record) = await env.Manager.TryGetForWritingAsync("produce-append-key", ct);
            await using (record)
            {
                var total = await env.Manager.DataStore.AppendAsync(record, async (stream, token) =>
                {
                    await using var writer = new StreamWriter(stream, Utf8NoBom, 1024, true);
                    await writer.WriteAsync("-tail".AsMemory(), token);
                }, ct);

                Assert.Equal(9, total);
            }

            Assert.Equal("head-tail", await env.ReadTextAsync("produce-append-key", ct));
        }

        [Fact]
        public async Task DataStore_PutAsync_WithProducer_PropagatesProducerFailure()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("produce-fail-key", ct);
            await using (record)
            {
                // The failure travels through the pipe: the store's read rethrows it, so the caller
                // sees the producer's own exception rather than a pipe-level one.
                await Assert.ThrowsAsync<InvalidTimeZoneException>(async () =>
                    await env.Manager.DataStore.PutAsync(record, (stream, token) =>
                        throw new InvalidTimeZoneException("producer gave up"), ct));
            }
        }

        [Fact]
        public async Task DataStore_PutAsync_WithProducer_SurvivesPayloadLargerThanPipeBuffer()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            // Well past the pipe's default pause threshold, so the producer really does get throttled
            // by the store rather than buffering everything up front.
            const int chunks = 400;
            var chunk = new string('x', 4096);

            var (_, record) = await env.Manager.TryGetOrSetAsync("produce-large-key", ct);
            long written;
            await using (record)
            {
                written = await env.Manager.DataStore.PutAsync(record, async (stream, token) =>
                {
                    await using var writer = new StreamWriter(stream, Utf8NoBom, 1024, true);
                    for (var i = 0; i < chunks; i++)
                    {
                        await writer.WriteAsync(chunk.AsMemory(), token);
                    }
                }, ct);
            }

            Assert.Equal(chunks * 4096, written);
        }

        [Fact]
        public async Task FileSystemDataStore_PutAsync_WithProducer_HandsOverTheDestinationStream()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("produce-direct-key", ct);
            await using (record)
            {
                await env.Manager.DataStore.PutAsync(record, (stream, token) =>
                {
                    // Store-specific, NOT a contract guarantee: this store owns a real file stream and
                    // overrides the default, so no pipe is in the way. The pipe default would hand
                    // over a non-seekable stream, which is why this pins the override being in effect.
                    Assert.True(stream.CanSeek);
                    return Task.CompletedTask;
                }, ct);
            }
        }

        [Fact]
        public async Task FileSystemDataStore_PutAsync_WithProducer_ReportsLengthWhenProducerSeeks()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("produce-seek-key", ct);
            long written;
            await using (record)
            {
                written = await env.Manager.DataStore.PutAsync(record, async (stream, token) =>
                {
                    var head = Encoding.UTF8.GetBytes("hello world");
                    await stream.WriteAsync(head, 0, head.Length, token);

                    // Rewind and overwrite in place: the final position is no longer the end, so the
                    // reported size has to come from the length.
                    stream.Seek(0, SeekOrigin.Begin);
                    var patch = Encoding.UTF8.GetBytes("HELLO");
                    await stream.WriteAsync(patch, 0, patch.Length, token);
                }, ct);
            }

            Assert.Equal(11, written);
            Assert.Equal("HELLO world", await env.ReadTextAsync("produce-seek-key", ct));
        }

        [Fact]
        public async Task DataStore_ReadAsync_ReturnsSeekableStream()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("seek-key", ct, content: "hello-world");

            var (_, record) = await env.Manager.TryGetForReadingAsync("seek-key", ct);
            await using (record)
            {
                await using var stream = await env.Manager.DataStore.ReadAsync(record, ct);

                // Pinned promise: a range is read by seeking, so a forward-only backend must wrap.
                Assert.True(stream.CanSeek);

                stream.Seek(6, SeekOrigin.Begin);
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, false);
                Assert.Equal("world", await reader.ReadToEndAsync(ct));
            }
        }

        [Fact]
        public async Task DataStore_PutAsync_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var record = new BlobRecord { Key = "no-lock-write", Metadata = "file.txt", LockType = LockType.Read };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.Manager.DataStore.PutAsync(record, Content("nope"), ct));
        }

        [Fact]
        public async Task DataStore_AppendAsync_AppendsToExistingContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("append-key", ct, content: "hello");

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("append-key", ct);
            await using (writeRecord)
            {
                // The returned size is the new total, not the appended length.
                Assert.Equal(11, await env.Manager.DataStore.AppendAsync(writeRecord, Content("-world"), ct));
            }

            Assert.Equal("hello-world", await env.ReadTextAsync("append-key", ct));
        }

        [Fact]
        public async Task DataStore_AppendAsync_NewKey_CreatesContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("append-new-key", ct);
            await using (record)
            {
                Assert.Equal(12, await env.Manager.DataStore.AppendAsync(record, Content("from-scratch"), ct));
            }

            Assert.Equal("from-scratch", await env.ReadTextAsync("append-new-key", ct));
        }

        [Fact]
        public async Task DataStore_AppendAsync_RequiresWriteLock()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("no-lock-append", ct);

            var record = new BlobRecord
            {
                Key = "no-lock-append",
                Metadata = "file.txt",
                LockType = LockType.Read
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await env.Manager.DataStore.AppendAsync(record, Content("nope"), ct);
            });
        }

        // ── Size ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task TryGetForReadingAsync_PopulatesSizeFromDataStore()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("size-key", ct, content: "hello-blob");

            var (ec, record) = await env.Manager.TryGetForReadingAsync("size-key", ct);
            await using (record)
            {
                Assert.Equal(BlobErrorCode.None, ec);
                Assert.Equal(10, record.Size);
            }
        }

        [Fact]
        public async Task DataStore_ExistsAsync_FollowsGetSizeAsync()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("exists-key", ct, content: "payload");
            var (_, seeded) = await env.Manager.TryGetForReadingAsync("exists-key", ct);
            await using (seeded)
            {
                Assert.True(await env.Manager.DataStore.ExistsAsync(seeded, ct));
            }

            // A record whose content was never written: GetSizeAsync yields null, so this is false.
            await using var reserved = await env.Manager.TryGetOrSetAsync("exists-missing-key", ct);
            Assert.Null(reserved.Record.Size);
            Assert.False(await env.Manager.DataStore.ExistsAsync(reserved.Record, ct));
        }

        [Fact]
        public async Task DataStore_ExistsAsync_TrueForEmptyContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            // Zero-byte content exists — absence is null, not a size of 0.
            await env.SeedAsync("exists-empty-key", ct);

            var (_, record) = await env.Manager.TryGetForReadingAsync("exists-empty-key", ct);
            await using (record)
            {
                Assert.Equal(0, record.Size);
                Assert.True(await env.Manager.DataStore.ExistsAsync(record, ct));
            }
        }

        [Fact]
        public async Task TryGetOrSetAsync_NewKey_SizeIsNull()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await using var result = await env.Manager.TryGetOrSetAsync("size-new-key", ct);

            Assert.True(result.IsNew);
            Assert.Null(result.Record.Size);
        }

        [Fact]
        public async Task Size_IsRefreshedAfterContentShrinks()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("size-shrink-key", ct, content: "original content");

            var (_, writeRecord) = await env.Manager.TryGetForWritingAsync("size-shrink-key", ct);
            await using (writeRecord)
            {
                Assert.Equal(16, writeRecord.Size);

                await env.Manager.DataStore.PutAsync(writeRecord, Content("new"), ct);

                // The store records the size as it writes, so the handle is current immediately.
                Assert.Equal(3, writeRecord.Size);
            }

            var (_, readRecord) = await env.Manager.TryGetForReadingAsync("size-shrink-key", ct);
            await using (readRecord)
            {
                Assert.Equal(3, readRecord.Size);
            }
        }

        [Fact]
        public async Task Size_PersistedOnWriteDispose_IsVisibleWithoutRereadingDataStore()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, record) = await env.Manager.TryGetOrSetAsync("size-persist-key", ct);
            await using (record)
            {
                await env.Manager.DataStore.PutAsync(record, Content("persisted"), ct);
            }

            // Going through the registry directly bypasses the data-store reconciliation, so
            // this asserts what the dispose callback actually persisted.
            var (_, persisted) = await env.Registry.TryGetForReadingAsync("size-persist-key", ct);
            await using (persisted)
            {
                Assert.Equal(9, persisted.Size);
            }
        }

        // ── Key → path mapping ───────────────────────────────────────────────────

        [Theory]
        // Differ only in a character a file name cannot carry, so a lossy sanitiser would fold them
        // together. The multi-segment branch has no hash in the path to save it.
        [InlineData("dir/a:b", "dir/a_b")]
        [InlineData("dir/a?b", "dir/a_b")]
        // '\' is an ordinary character, not a separator, so this is a flat key rather than 'a' + 'b'.
        [InlineData("coll\\b", "coll/b")]
        // Windows trims a trailing dot and space, which would alias these onto the bare name.
        [InlineData("dir/trail.", "dir/trail")]
        [InlineData("dir/space ", "dir/space")]
        // The escape character itself has to be escaped, or an escaped form could be forged.
        [InlineData("dir/pct%3Ax", "dir/pct:x")]
        public async Task DistinctKeys_NeverShareContent(string first, string second)
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync(first, ct, content: "first");
            await env.SeedAsync(second, ct, content: "second");

            Assert.Equal("first", await env.ReadTextAsync(first, ct));
            Assert.Equal("second", await env.ReadTextAsync(second, ct));
        }

        [Fact]
        public async Task Key_WithSeparators_KeepsItsFileExtension()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            // Escaping must not disturb an ordinary name: the extension still has to survive, since
            // ResolveLocationAsync is what callers hand to anything that inspects it.
            await env.SeedAsync("reports/2026/august.png", ct, content: "payload");

            var location = await env.LocateAsync("reports/2026/august.png", ct);

            Assert.EndsWith(".png", location, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("reports", location);
        }

        // ── BlobRecord.Apply ─────────────────────────────────────────────────────

        [Fact]
        public async Task Apply_UnderWriteLock_PersistsMetadataOnDispose()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("apply-key", ct, content: "payload");

            // Updating an existing blob's metadata needs only the write lock it was handed out with —
            // no round trip through TryGetOrSetAsync.
            var (_, record) = await env.Manager.TryGetForWritingAsync("apply-key", ct);
            await using (record)
            {
                record.Apply(new BlobStoreOptions { ContentType = "image/png", Metadata = "photo.png" });
            }

            var (_, updated) = await env.Manager.TryGetForReadingAsync("apply-key", ct);
            await using (updated)
            {
                Assert.Equal("image/png", updated.ContentType);
                Assert.Equal("photo.png", updated.Metadata);
            }
        }

        [Fact]
        public async Task Apply_ResolvesTtlAgainstCurrentTime()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("apply-ttl-key", ct, content: "payload");

            var (_, record) = await env.Manager.TryGetForWritingAsync("apply-ttl-key", ct);
            await using (record)
            {
                // Ttl is an instruction, not state: the record itself has no place to hold it.
                record.Apply(new BlobStoreOptions { Ttl = TimeSpan.FromHours(1) });

                var expected = DateTimeOffset.UtcNow.AddHours(1);
                Assert.NotNull(record.ExpiresAt);
                Assert.True(record.ExpiresAt.Value >= expected.AddSeconds(-5));
                Assert.True(record.ExpiresAt.Value <= expected.AddSeconds(5));
            }
        }

        [Fact]
        public async Task Apply_LeavesValuesThatWereNotSet()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("apply-partial-key", ct, options: new BlobStoreOptions
            {
                ContentType = "text/plain",
                Metadata = "notes.txt"
            }, content: "payload");

            var (_, record) = await env.Manager.TryGetForWritingAsync("apply-partial-key", ct);
            await using (record)
            {
                record.Apply(new BlobStoreOptions { Metadata = "renamed.txt" });

                Assert.Equal("renamed.txt", record.Metadata);
                Assert.Equal("text/plain", record.ContentType);
            }
        }

        [Fact]
        public async Task Apply_UnderReadLock_Throws()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("apply-readlock-key", ct, content: "payload");

            var (_, record) = await env.Manager.TryGetForReadingAsync("apply-readlock-key", ct);
            await using (record)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    record.Apply(new BlobStoreOptions { ContentType = "image/png" }));
            }
        }

        [Fact]
        public async Task TryGetForWritingAsync_WithOptions_PersistsMetadataOnDispose()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("write-options-key", ct, content: "payload");

            var (ec, record) = await env.Manager.TryGetForWritingAsync(
                "write-options-key",
                new BlobStoreOptions { ContentType = "image/png", Metadata = "photo.png", Ttl = TimeSpan.FromHours(1) },
                ct);

            Assert.Equal(BlobErrorCode.None, ec);
            await using (record)
            {
                Assert.Equal("image/png", record.ContentType);

                var expected = DateTimeOffset.UtcNow.AddHours(1);
                Assert.NotNull(record.ExpiresAt);
                Assert.True(record.ExpiresAt.Value >= expected.AddSeconds(-5));
                Assert.True(record.ExpiresAt.Value <= expected.AddSeconds(5));
            }

            var (_, updated) = await env.Manager.TryGetForReadingAsync("write-options-key", ct);
            await using (updated)
            {
                Assert.Equal("image/png", updated.ContentType);
                Assert.Equal("photo.png", updated.Metadata);
                Assert.NotNull(updated.ExpiresAt);
            }
        }

        [Fact]
        public async Task TryGetForWritingAsync_WithOptions_KeyNotFound_ReturnsKeyNotFound()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            // No record, so there is nothing to apply the options to — and no handle to dispose.
            var (ec, record) = await env.Manager.TryGetForWritingAsync(
                "nonexistent",
                new BlobStoreOptions { ContentType = "image/png" },
                ct);

            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
            Assert.Null(record);
        }

        [Fact]
        public async Task TryGetForWritingAsync_WithOptions_Locked_ReturnsTimeout()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("write-options-locked", ct, content: "payload");

            var (_, held) = await env.Manager.TryGetForWritingAsync("write-options-locked", ct);
            await using (held)
            {
                var (ec, record) = await env.Manager.TryGetForWritingAsync(
                    "write-options-locked",
                    new BlobStoreOptions { ContentType = "image/png" },
                    TimeSpan.Zero,
                    ct);

                Assert.Equal(BlobErrorCode.Timeout, ec);
                Assert.Null(record);
            }

            // The failed acquisition must not have applied anything.
            var (_, unchanged) = await env.Manager.TryGetForReadingAsync("write-options-locked", ct);
            await using (unchanged)
            {
                Assert.Null(unchanged.ContentType);
            }
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

            await env.SeedAsync("wlock-key", ct);

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

            await env.SeedAsync("multi-read-key", ct);

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

            await env.SeedAsync("existing-key", ct);

            await using var result = await env.Manager.TryGetOrSetAsync("existing-key", ct);

            Assert.False(result.IsNew);
        }

        [Fact]
        public async Task TryGetOrSetAsync_RecordWithoutContent_ReportsIsNew()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            // Registers the record only — nothing writes the blob itself.
            var (_, reserved) = await env.Manager.TryGetOrSetAsync("no-content-key", ct);
            await using (reserved) { }

            await using var result = await env.Manager.TryGetOrSetAsync("no-content-key", ct);

            Assert.Equal(BlobErrorCode.None, result.ErrorCode);
            Assert.True(result.IsNew);
        }

        [Fact]
        public async Task TryGetOrSetAsync_ContentLostExternally_ReportsIsNew()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("lost-content-key", ct, content: "payload");

            string location;
            var (_, probe) = await env.Manager.TryGetForReadingAsync("lost-content-key", ct);
            await using (probe)
            {
                location = await env.Manager.DataStore.ResolveLocationAsync(probe, ct);
            }
            File.Delete(location);

            await using var result = await env.Manager.TryGetOrSetAsync("lost-content-key", ct);

            Assert.Equal(BlobErrorCode.None, result.ErrorCode);
            Assert.True(result.IsNew);
        }

        [Fact]
        public async Task TryGetForReadingAsync_ContentMissing_ReturnsKeyNotFoundAndDropsRecord()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var (_, reserved) = await env.Manager.TryGetOrSetAsync("orphan-key", ct);
            await using (reserved) { }

            var (ec, record) = await env.Manager.TryGetForReadingAsync("orphan-key", ct);

            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
            Assert.Null(record);
            Assert.Empty(await env.Manager.QueryAsync("orphan-key", ct));
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

            await env.SeedAsync("delete-key", ct);

            await env.Manager.DeleteAsync("delete-key", ct);

            var (ec, _) = await env.Manager.TryGetForReadingAsync("delete-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
        }

        [Fact]
        public async Task DeleteAsync_WithTimeout_LockedRecord_ThrowsTimeoutException()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("delete-locked-key", ct);

            var (readEc, read) = await env.Manager.TryGetForReadingAsync("delete-locked-key", ct);
            Assert.Equal(BlobErrorCode.None, readEc);
            await using (read)
            {
                await Assert.ThrowsAsync<TimeoutException>(async () =>
                    await env.Manager.DeleteAsync("delete-locked-key", TimeSpan.FromMilliseconds(200), ct));
            }
        }

        [Fact]
        public async Task DeleteAsync_KeyNotFound_ThrowsKeyNotFoundException()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await env.Manager.DeleteAsync("nonexistent-delete", ct));
        }

        [Fact]
        public async Task DeleteAsync_RemovesContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("delete-content-key", ct, content: "payload");
            var location = await env.LocateAsync("delete-content-key", ct);
            Assert.True(File.Exists(location));

            await env.Manager.DeleteAsync("delete-content-key", ct);

            Assert.False(File.Exists(location));
        }

        [Fact]
        public async Task DeleteAsync_PrunesEmptiedShardDirectories()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("shard-key", ct, content: "payload");
            Assert.NotEmpty(Directory.GetFileSystemEntries(env.DataPath));

            await env.Manager.DeleteAsync("shard-key", ct);

            Assert.Empty(Directory.GetFileSystemEntries(env.DataPath));
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
            await env.SeedAsync("fresh-key", ct, options);

            await env.Manager.DeleteExpiredAsync(ct);

            var (ec, fresh) = await env.Manager.TryGetForReadingAsync("fresh-key", ct);
            Assert.Equal(BlobErrorCode.None, ec);
            await using (fresh) { }
        }

        [Fact]
        public async Task DeleteExpiredAsync_RemovesContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            var options = new BlobStoreOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(-2) };
            await env.SeedAsync("expired-content-key", ct, options, "payload");
            var location = await env.LocateAsync("expired-content-key", ct);
            Assert.True(File.Exists(location));

            var deleted = await env.Manager.DeleteExpiredAsync(ct);

            Assert.True(deleted > 0);
            Assert.False(File.Exists(location));
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

            await env.SeedAsync("locked-old-key", ct, content: "payload");
            var location = await env.LocateAsync("locked-old-key", ct);

            var (_, record) = await env.Manager.TryGetForWritingAsync("locked-old-key", ct);
            await using (record)
            {
                var elapsed = Stopwatch.StartNew();
                var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct);
                elapsed.Stop();

                Assert.Equal(0, deleted);

                // Bulk deletion passes TimeSpan.Zero, which must attempt the lock once and give up.
                // Falling back to the registry default (2 s for this environment) would be a regression.
                Assert.True(
                    elapsed.Elapsed < TimeSpan.FromSeconds(1),
                    $"skipping a locked record waited {elapsed.Elapsed}");
            }

            var (ec, _) = await env.Manager.TryGetForReadingAsync("locked-old-key", ct);
            Assert.Equal(BlobErrorCode.None, ec);
            Assert.True(File.Exists(location));
        }

        [Fact]
        public async Task DeleteOlderThanAsync_ForceDeleteLocked_RemovesLockedRecords()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("force-old-key", ct, content: "payload");
            var location = await env.LocateAsync("force-old-key", ct);

            var (_, record) = await env.Manager.TryGetForWritingAsync("force-old-key", ct);
            await using (record)
            {
                var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct, forceDeleteLocked: true);

                Assert.True(deleted > 0);
            }

            var (ec, _) = await env.Manager.TryGetForReadingAsync("force-old-key", ct);
            Assert.Equal(BlobErrorCode.KeyNotFound, ec);
            Assert.False(File.Exists(location));
        }

        [Fact]
        public async Task DeleteOlderThanAsync_RemovesContent()
        {
            var ct = TestContext.Current.CancellationToken;
            await using var env = new TestEnvironment();

            await env.SeedAsync("old-content-key", ct, content: "payload");
            var location = await env.LocateAsync("old-content-key", ct);
            Assert.True(File.Exists(location));

            var deleted = await env.Manager.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddSeconds(5), ct);

            Assert.True(deleted > 0);
            Assert.False(File.Exists(location));
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

            public SQLiteBlobRegistry Registry { get; }

            public string DataPath => _dataPath;

            public async Task<string> ReadTextAsync(string key, CancellationToken ct)
            {
                var (_, record) = await Manager.TryGetForReadingAsync(key, ct);
                await using (record)
                {
                    await using var stream = await Manager.DataStore.ReadAsync(record, ct);
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, false);
                    return await reader.ReadToEndAsync(ct);
                }
            }

            /// <summary>
            /// Resolves the on-disk location while the content still exists, so a test can assert
            /// on it after deletion.
            /// </summary>
            public async Task<string> LocateAsync(string key, CancellationToken ct)
            {
                var (_, record) = await Manager.TryGetForReadingAsync(key, ct);
                await using (record)
                {
                    return await Manager.DataStore.ResolveLocationAsync(record, ct);
                }
            }

            public TestEnvironment()
                : this(TimeSpan.FromSeconds(2))
            {
            }

            public TestEnvironment(TimeSpan defaultTimeout)
            {
                _dbPath = Path.Combine(Path.GetTempPath(), "blob_manager_" + Guid.NewGuid().ToString("N") + ".db");
                _dataPath = Path.Combine(Path.GetTempPath(), "blob_manager_files_" + Guid.NewGuid().ToString("N"));

                Registry = new SQLiteBlobRegistry(_dbPath, defaultTimeout);
                var dataStore = new FileSystemBlobDataStore(_dataPath);
                Manager = new BlobManager(dataStore, Registry);
            }

            /// <summary>
            /// Creates a blob together with its content. A registry record without content is a
            /// transient state, so any test that later expects the blob to be retrievable via
            /// TryGetForReadingAsync / TryGetForWritingAsync has to seed both.
            /// </summary>
            public async Task SeedAsync(string key, CancellationToken ct, BlobStoreOptions options = null, string content = "")
            {
                var (_, record) = options == null
                    ? await Manager.TryGetOrSetAsync(key, ct)
                    : await Manager.TryGetOrSetAsync(key, options, LockType.Write, ct);

                await using (record)
                {
                    await Manager.DataStore.PutAsync(record, Content(content), ct);
                }
            }

            public ValueTask DisposeAsync()
            {
                try
                {
                    if (File.Exists(_dbPath))
                    {
                        File.Delete(_dbPath);
                    }
                }
                catch { }

                try
                {
                    if (Directory.Exists(_dataPath))
                    {
                        Directory.Delete(_dataPath, true);
                    }
                }
                catch { }

                return ValueTask.CompletedTask;
            }
        }
    }
}
