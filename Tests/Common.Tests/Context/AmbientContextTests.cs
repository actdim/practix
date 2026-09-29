using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Abstractions.Context.Extensions;
using ActDim.Practix.Abstractions.Logging;
using ActDim.Practix.Abstractions.Memory;
using ActDim.Practix.Abstractions.Storage;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Context
{
    public class AmbientContextTests
    {
        [Fact]
        public void AmbientContext_PushProperty_SetsAndRestoresValues()
        {
            var context = AmbientContext.Current;

            Assert.False(context.Properties.ContainsKey("TenantId"));

            using (context.PushProperty("TenantId", "Tenant_1"))
            {
                Assert.Equal("Tenant_1", context.Properties["TenantId"]);
                Assert.Equal("Tenant_1", AmbientContext.CurrentProperties["TenantId"]);

                using (context.PushProperty("TenantId", "Tenant_2"))
                {
                    Assert.Equal("Tenant_2", context.Properties["TenantId"]);
                }

                Assert.Equal("Tenant_1", context.Properties["TenantId"]);
            }

            Assert.False(context.Properties.ContainsKey("TenantId"));
        }

        [Fact]
        public async Task AmbientContext_FlowsAcrossAsyncCalls_WithoutCrossTaskPollution()
        {
            var context = AmbientContext.Current;

            using (AmbientContext.Push("FlowId", "MainFlow"))
            {
                Assert.Equal("MainFlow", context.Properties["FlowId"]);

                var task1 = Task.Run(async () =>
                {
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                    using (AmbientContext.Push("FlowId", "Branch_1"))
                    {
                        await Task.Yield();
                        Assert.Equal("Branch_1", context.Properties["FlowId"]);
                    }
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                }, TestContext.Current.CancellationToken);

                var task2 = Task.Run(async () =>
                {
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                    using (AmbientContext.Push("FlowId", "Branch_2"))
                    {
                        await Task.Yield();
                        Assert.Equal("Branch_2", context.Properties["FlowId"]);
                    }
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                }, TestContext.Current.CancellationToken);

                await Task.WhenAll(task1, task2);

                Assert.Equal("MainFlow", context.Properties["FlowId"]);
            }

            Assert.False(context.Properties.ContainsKey("FlowId"));
        }

        [Fact]
        public void Services_ReturnsNullWhenNoServicesConfigured()
        {
            Assert.Null(AmbientContext.Current.GetServices());
        }

        [Fact]
        public void Services_ResolvesFromAmbientScopeAndRestores()
        {
            var services1 = new ServiceCollection().BuildServiceProvider();
            var services2 = new ServiceCollection().BuildServiceProvider();

            using (AmbientContext.Current.WithServices(services1))
            {
                Assert.Same(services1, AmbientContext.Current.GetServices());

                using (AmbientContext.Current.WithServices(services2))
                {
                    Assert.Same(services2, AmbientContext.Current.GetServices());
                }

                Assert.Same(services1, AmbientContext.Current.GetServices());
            }

            Assert.Null(AmbientContext.Current.GetServices());
        }

        [Fact]
        public void User_ResolvesNullByDefault_AndSupportsScopedOverrides()
        {
            var defaultUser = AmbientContext.Current.GetUser();
            Assert.Null(defaultUser);

            var user1 = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "TestAuth"));
            var user2 = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Bob")], "TestAuth"));

            using (AmbientContext.Current.WithUser(user1))
            {
                Assert.Equal("Alice", AmbientContext.Current.GetUser()?.Identity?.Name);

                using (AmbientContext.Current.WithUser(user2))
                {
                    Assert.Equal("Bob", AmbientContext.Current.GetUser()?.Identity?.Name);
                }

                Assert.Equal("Alice", AmbientContext.Current.GetUser()?.Identity?.Name);
            }

            Assert.Null(AmbientContext.Current.GetUser());
        }

        [Fact]
        public void CancellationToken_ResolvesNullByDefault_AndSupportsScopedOverrides()
        {
            Assert.Null(AmbientContext.Current.GetCancellationToken());

            using var cts = new CancellationTokenSource();
            using (AmbientContext.Current.WithCancellationToken(cts.Token))
            {
                Assert.Equal(cts.Token, AmbientContext.Current.GetCancellationToken());
                Assert.False(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);

                cts.Cancel();
                Assert.True(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);
            }

            Assert.Null(AmbientContext.Current.GetCancellationToken());
        }

        [Fact]
        public async Task WithTimeout_CancelsTokenAfterDuration_AndDisposesCleanly()
        {
            Assert.Null(AmbientContext.Current.GetCancellationToken());

            CancellationToken timeoutToken;
            using (AmbientContext.Current.WithTimeout(TimeSpan.FromMilliseconds(50), out timeoutToken))
            {
                Assert.Equal(timeoutToken, AmbientContext.Current.GetCancellationToken());
                Assert.False(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);

                // Await the cancellation itself instead of polling with a short wall-clock budget:
                // on loaded CI runners the timer callback can be delayed well beyond the nominal timeout.
                var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using (timeoutToken.Register(() => cancelled.TrySetResult()))
                {
                    await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
                }
                Assert.True(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);
                Assert.True(timeoutToken.IsCancellationRequested);
            }

            Assert.Null(AmbientContext.Current.GetCancellationToken());
        }

        [Fact]
        public void Blobs_ResolvesFromAmbientOverride()
        {
            var testBlobManager1 = new TestBlobManager();
            var testBlobManager2 = new TestBlobManager();

            using (AmbientContext.Current.WithBlobManager(testBlobManager1))
            {
                Assert.Same(testBlobManager1, AmbientContext.Current.GetBlobManager());

                using (AmbientContext.Current.WithBlobManager(testBlobManager2))
                {
                    Assert.Same(testBlobManager2, AmbientContext.Current.GetBlobManager());
                }

                Assert.Same(testBlobManager1, AmbientContext.Current.GetBlobManager());
            }

            Assert.Null(AmbientContext.Current.GetBlobManager());
        }

        [Fact]
        public void Compression_ResolvesFromAmbientOverride()
        {
            var testCompression1 = new TestCompressionManager();
            var testCompression2 = new TestCompressionManager();

            using (AmbientContext.Current.WithCompressionManager(testCompression1))
            {
                Assert.Same(testCompression1, AmbientContext.Current.GetCompressionManager());

                using (AmbientContext.Current.WithCompressionManager(testCompression2))
                {
                    Assert.Same(testCompression2, AmbientContext.Current.GetCompressionManager());
                }

                Assert.Same(testCompression1, AmbientContext.Current.GetCompressionManager());
            }

            Assert.Null(AmbientContext.Current.GetCompressionManager());
        }

        [Fact]
        public void Memory_ResolvesFromAmbientOverride()
        {
            Assert.Null(AmbientContext.Current.GetMemoryManager());

            var customManager = new Microsoft.IO.RecyclableMemoryStreamManager();

            using (AmbientContext.Current.WithMemoryManager(customManager))
            {
                Assert.Same(customManager, AmbientContext.Current.GetMemoryManager());
            }

            Assert.Null(AmbientContext.Current.GetMemoryManager());
        }

        [Fact]
        public void Logging_ResolvesLoggerFactory_AndSupportsScopedOverrides()
        {
            Assert.Null(AmbientContext.Current.GetLoggerFactory());

            var customFactory = new TestLoggerFactory();

            using (AmbientContext.Current.WithLoggerFactory(customFactory))
            {
                Assert.Same(customFactory, AmbientContext.Current.GetLoggerFactory());
            }

            Assert.Null(AmbientContext.Current.GetLoggerFactory());
        }

        [Fact]
        public void IAmbientContextExtensions_WorkOnInterfaceDirectly()
        {
            var context = AmbientContext.Current;
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "DirectUser")], "DirectAuth"));
            var testBlob = new TestBlobManager();
            var testFactory = new TestLoggerFactory();
            var services = new ServiceCollection().BuildServiceProvider();
            using var cts = new CancellationTokenSource();

            using (context.WithUser(user))
            using (context.WithBlobManager(testBlob))
            using (context.WithLoggerFactory(testFactory))
            using (context.WithServices(services))
            using (context.WithCancellationToken(cts.Token))
            {
                Assert.Same(user, context.GetUser());
                Assert.Same(testBlob, context.GetBlobManager());
                Assert.Same(testFactory, context.GetLoggerFactory());
                Assert.Same(services, context.GetServices());
                Assert.Equal(cts.Token, context.GetCancellationToken());
            }

            Assert.Null(context.GetUser());
            Assert.Null(context.GetBlobManager());
            Assert.Null(context.GetLoggerFactory());
            Assert.Null(context.GetServices());
            Assert.Null(context.GetCancellationToken());
        }

        [Fact]
        public void WithCancellationToken_CombinesWithExistingAmbientToken_UsingLinkedTokenSource()
        {
            using var parentCts = new CancellationTokenSource();
            using var childCts = new CancellationTokenSource();

            using (AmbientContext.Current.WithCancellationToken(parentCts.Token))
            {
                Assert.Equal(parentCts.Token, AmbientContext.Current.GetCancellationToken());
                Assert.False(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);

                // Combine existing ambient token with child token via LinkedTokenSource
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(AmbientContext.Current.GetCancellationToken() ?? CancellationToken.None, childCts.Token);
                using (AmbientContext.Current.WithCancellationToken(linkedCts.Token))
                {
                    Assert.Equal(linkedCts.Token, AmbientContext.Current.GetCancellationToken());
                    Assert.False(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);

                    // Child cancellation propagates to ambient context
                    childCts.Cancel();
                    Assert.True(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);
                    Assert.False(parentCts.IsCancellationRequested);
                }

                // Exiting inner scope restores un-cancelled parent token
                Assert.Equal(parentCts.Token, AmbientContext.Current.GetCancellationToken());
                Assert.False(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);

                // Parent cancellation propagates to new linked scope
                using var linkedCts2 = CancellationTokenSource.CreateLinkedTokenSource(AmbientContext.Current.GetCancellationToken() ?? CancellationToken.None, CancellationToken.None);
                using (AmbientContext.Current.WithCancellationToken(linkedCts2.Token))
                {
                    parentCts.Cancel();
                    Assert.True(AmbientContext.Current.GetCancellationToken()!.Value.IsCancellationRequested);
                }
            }
        }

        private sealed class TestBlobManager : IBlobManager
        {
            public IBlobDataStore this[string key] => throw new NotImplementedException();
            public IReadOnlyList<IBlobDataStore> DataStores => throw new NotImplementedException();
            public IBlobDataStore GetDataStore(string key) => throw new NotImplementedException();
            public Task<BlobResult> TryGetOrSetAsync(string key, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, CancellationToken ct) => throw new NotImplementedException();
            public Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, TimeSpan timeout, CancellationToken ct) => throw new NotImplementedException();
            public Task<IList<string>> QueryAsync(string pattern, CancellationToken ct) => Task.FromResult<IList<string>>([]);
            public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
            public Task DeleteAsync(string key, TimeSpan timeout, CancellationToken ct) => Task.CompletedTask;
            public Task<int> DeleteExpiredAsync(CancellationToken ct) => Task.FromResult(0);
            public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false) => Task.FromResult(0);
            public Task CleanupAsync(CancellationToken ct) => Task.CompletedTask;
        }

        private sealed class TestLoggerFactory : ILoggerFactory
        {
            public ILogger? LastCreatedLogger { get; private set; }

            public void AddProvider(ILoggerProvider provider) { }

            public ILogger CreateLogger(string categoryName)
            {
                var logger = new TestLogger(categoryName);
                LastCreatedLogger = logger;
                return logger;
            }

            public void Dispose() { }
        }

        private sealed class TestLogger : ILogger
        {
            public string CategoryName { get; }

            public TestLogger(string categoryName)
            {
                CategoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }

        private sealed class TestCompressionManager : ActDim.Practix.Abstractions.Compression.ICompressionManager
        {
            public ActDim.Practix.Abstractions.Compression.ArchiveFormat GetArchiveFormat(ReadOnlyMemory<byte> data) => throw new NotImplementedException();
            public ActDim.Practix.Abstractions.Compression.ArchiveFormat GetArchiveFormat(System.IO.Stream stream) => throw new NotImplementedException();
            public ActDim.Practix.Abstractions.Compression.CompressionFormat GetCompressionFormat(ReadOnlyMemory<byte> data) => throw new NotImplementedException();
            public ActDim.Practix.Abstractions.Compression.CompressionFormat GetCompressionFormat(System.IO.Stream stream) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressAsync(ReadOnlyMemory<byte> data, ActDim.Practix.Abstractions.Compression.CompressionFormat compressionFormat, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressAsync(System.IO.Stream stream, ActDim.Practix.Abstractions.Compression.CompressionFormat compressionFormat, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> DecompressAsync(ReadOnlyMemory<byte> data, ActDim.Practix.Abstractions.Compression.CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<byte[]> DecompressAsync(System.IO.Stream stream, ActDim.Practix.Abstractions.Compression.CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DecompressAsync(ReadOnlyMemory<byte> data, System.IO.Stream outputStream, ActDim.Practix.Abstractions.Compression.CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DecompressAsync(System.IO.Stream stream, System.IO.Stream outputStream, ActDim.Practix.Abstractions.Compression.CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DecompressArchiveAsync(ReadOnlyMemory<byte> data, ActDim.Practix.Abstractions.Compression.ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DecompressArchiveAsync(System.IO.Stream stream, ActDim.Practix.Abstractions.Compression.ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IList<ActDim.Practix.Abstractions.Compression.IArchiveEntry>> GetArchiveEntriesAsync(System.IO.Stream stream, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IList<ActDim.Practix.Abstractions.Compression.IArchiveEntry>> GetArchiveEntriesAsync(ReadOnlyMemory<byte> data, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressToArchiveAsync(System.IO.Stream outputStream, IEnumerable<ActDim.Practix.Abstractions.Compression.ArchiveEntrySource> sources, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressToArchiveAsync(System.IO.Stream outputStream, IEnumerable<ActDim.Practix.Abstractions.Compression.ArchiveEntrySource> sources, ActDim.Practix.Abstractions.Compression.ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressToArchiveAsync(IEnumerable<ActDim.Practix.Abstractions.Compression.ArchiveEntrySource> sources, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<System.IO.Stream> CompressToArchiveAsync(IEnumerable<ActDim.Practix.Abstractions.Compression.ArchiveEntrySource> sources, ActDim.Practix.Abstractions.Compression.ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public ActDim.Practix.Abstractions.Compression.ArchiveFormat GetArchiveFormatByFileExtension(string ext) => throw new NotImplementedException();
            public string FixArchiveFileExtension(string fileName, ActDim.Practix.Abstractions.Compression.ArchiveFormat? archiveFormat = default) => throw new NotImplementedException();
        }
    }
}
