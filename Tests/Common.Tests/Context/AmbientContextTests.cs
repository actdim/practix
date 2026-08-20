using ActDim.BytePath;
using ActDim.Practix.Abstractions.Context;
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
        public void Services_ThrowsWhenNoServicesConfigured()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = AmbientContext.Services;
            });
        }

        [Fact]
        public void Services_ResolvesFromAmbientScopeAndRestores()
        {
            var services1 = new ServiceCollection().BuildServiceProvider();
            var services2 = new ServiceCollection().BuildServiceProvider();

            using (AmbientContext.WithServices(services1))
            {
                Assert.Same(services1, AmbientContext.Services);

                using (AmbientContext.WithServices(services2))
                {
                    Assert.Same(services2, AmbientContext.Services);
                }

                Assert.Same(services1, AmbientContext.Services);
            }

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = AmbientContext.Services;
            });
        }

        [Fact]
        public void User_ResolvesAnonymousByDefault_AndSupportsScopedOverrides()
        {
            var defaultUser = AmbientContext.User;
            Assert.NotNull(defaultUser);
            Assert.False(defaultUser.Identity?.IsAuthenticated ?? false);

            var user1 = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "TestAuth"));
            var user2 = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Bob")], "TestAuth"));

            using (AmbientContext.WithUser(user1))
            {
                Assert.Equal("Alice", AmbientContext.User.Identity?.Name);

                using (AmbientContext.WithUser(user2))
                {
                    Assert.Equal("Bob", AmbientContext.User.Identity?.Name);
                }

                Assert.Equal("Alice", AmbientContext.User.Identity?.Name);
            }

            Assert.False(AmbientContext.User.Identity?.IsAuthenticated ?? false);
        }

        [Fact]
        public void CancellationToken_ResolvesNoneByDefault_AndSupportsScopedOverrides()
        {
            Assert.Equal(CancellationToken.None, AmbientContext.CancellationToken);

            using var cts = new CancellationTokenSource();
            using (AmbientContext.WithCancellationToken(cts.Token))
            {
                Assert.Equal(cts.Token, AmbientContext.CancellationToken);
                Assert.False(AmbientContext.CancellationToken.IsCancellationRequested);

                cts.Cancel();
                Assert.True(AmbientContext.CancellationToken.IsCancellationRequested);
            }

            Assert.Equal(CancellationToken.None, AmbientContext.CancellationToken);
        }

        [Fact]
        public async Task WithTimeout_CancelsTokenAfterDuration_AndDisposesCleanly()
        {
            Assert.Equal(CancellationToken.None, AmbientContext.CancellationToken);

            CancellationToken timeoutToken;
            using (AmbientContext.WithTimeout(TimeSpan.FromMilliseconds(50), out timeoutToken))
            {
                Assert.Equal(timeoutToken, AmbientContext.CancellationToken);
                Assert.False(AmbientContext.CancellationToken.IsCancellationRequested);

                await Task.Delay(100, TestContext.Current.CancellationToken);
                Assert.True(AmbientContext.CancellationToken.IsCancellationRequested);
                Assert.True(timeoutToken.IsCancellationRequested);
            }

            Assert.Equal(CancellationToken.None, AmbientContext.CancellationToken);
        }

        [Fact]
        public void Blobs_ResolvesFromAmbientOverride_OrFromServices()
        {
            var testBlobManager1 = new TestBlobManager();
            var testBlobManager2 = new TestBlobManager();

            var services = new ServiceCollection()
                .AddSingleton<IBlobManager>(testBlobManager1)
                .BuildServiceProvider();

            using (AmbientContext.WithServices(services))
            {
                Assert.Same(testBlobManager1, AmbientContext.Blobs);

                using (AmbientContext.WithBlobManager(testBlobManager2))
                {
                    Assert.Same(testBlobManager2, AmbientContext.Blobs);
                }

                Assert.Same(testBlobManager1, AmbientContext.Blobs);
            }
        }

        [Fact]
        public void Compression_ResolvesFromAmbientOverride_OrFromServices()
        {
            var testCompression1 = new TestCompressionManager();
            var testCompression2 = new TestCompressionManager();

            var services = new ServiceCollection()
                .AddSingleton<ActDim.Practix.Abstractions.Compression.ICompressionManager>(testCompression1)
                .BuildServiceProvider();

            using (AmbientContext.WithServices(services))
            {
                Assert.Same(testCompression1, AmbientContext.Compression);

                using (AmbientContext.WithCompressionManager(testCompression2))
                {
                    Assert.Same(testCompression2, AmbientContext.Compression);
                }

                Assert.Same(testCompression1, AmbientContext.Compression);
            }
        }

        [Fact]
        public void Memory_ResolvesFromAmbientOverride_OrDefaultsToProcessManager()
        {
            var defaultMemory = AmbientContext.Memory;
            Assert.NotNull(defaultMemory);
            Assert.Same(ActDim.Practix.Common.Memory.MemoryManager.Default, defaultMemory);

            var customManager = new Microsoft.IO.RecyclableMemoryStreamManager();

            using (AmbientContext.WithMemoryManager(customManager))
            {
                Assert.Same(customManager, AmbientContext.Memory);
                Assert.Same(customManager, AmbientContext.Current.GetMemoryManager());
            }

            Assert.Same(ActDim.Practix.Common.Memory.MemoryManager.Default, AmbientContext.Memory);
        }

        [Fact]
        public void Logging_ResolvesLoggerFactory_AndSupportsScopedOverrides()
        {
            var logger = AmbientContext.Log<AmbientContextTests>();
            Assert.NotNull(logger);

            var customFactory = new TestLoggerFactory();

            using (AmbientContext.WithLoggerFactory(customFactory))
            {
                Assert.Same(customFactory, AmbientContext.LoggerFactory);

                var logInstance = AmbientContext.Log(this);
                Assert.NotNull(logInstance);
                Assert.Same(customFactory.LastCreatedLogger, logInstance);

                var logType = AmbientContext.Log(typeof(AmbientContextTests));
                Assert.NotNull(logType);
            }
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

            using (AmbientContext.WithCancellationToken(parentCts.Token))
            {
                Assert.Equal(parentCts.Token, AmbientContext.CancellationToken);
                Assert.False(AmbientContext.CancellationToken.IsCancellationRequested);

                // Combine existing ambient token with child token via LinkedTokenSource
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(AmbientContext.CancellationToken, childCts.Token);
                using (AmbientContext.WithCancellationToken(linkedCts.Token))
                {
                    Assert.Equal(linkedCts.Token, AmbientContext.CancellationToken);
                    Assert.False(AmbientContext.CancellationToken.IsCancellationRequested);

                    // Child cancellation propagates to ambient context
                    childCts.Cancel();
                    Assert.True(AmbientContext.CancellationToken.IsCancellationRequested);
                    Assert.False(parentCts.IsCancellationRequested);
                }

                // Exiting inner scope restores un-cancelled parent token
                Assert.Equal(parentCts.Token, AmbientContext.CancellationToken);
                Assert.False(AmbientContext.CancellationToken.IsCancellationRequested);

                // Parent cancellation propagates to new linked scope
                using var linkedCts2 = CancellationTokenSource.CreateLinkedTokenSource(AmbientContext.CancellationToken, CancellationToken.None);
                using (AmbientContext.WithCancellationToken(linkedCts2.Token))
                {
                    parentCts.Cancel();
                    Assert.True(AmbientContext.CancellationToken.IsCancellationRequested);
                }
            }
        }

        private sealed class TestBlobManager : IBlobManager
        {
            public IBlobDataStore DataStore => throw new NotImplementedException();
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
