using ActDim.Practix.Caching;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Caching
{
    public class MemoryCachingProxyTests
    {
        private readonly MemoryCache _cache;
        private readonly MemoryCachingProxy _proxy;
        private static readonly MemoryCacheEntryOptions _options = new();

        public MemoryCachingProxyTests()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _proxy = new MemoryCachingProxy(_cache);
        }

        public class Payload
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        // ── Constructor / argument guards ────────────────────────────────────────

        [Fact]
        public void Ctor_NullCache_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new MemoryCachingProxy(null));

        [Fact]
        public void Get_NullFunc_Throws() =>
            Assert.Throws<ArgumentNullException>(() => _proxy.Get<int>(null, _options));

        // ── Synchronous memoization ──────────────────────────────────────────────

        [Fact]
        public void Get_FirstCall_InvokesFuncAndReturnsValue()
        {
            var calls = 0;
            var cached = _proxy.Get<int>(k => { calls++; return 42; }, _options);

            var result = cached("key");

            Assert.Equal(42, result);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Get_SecondCallSameKey_ServesFromCacheWithoutInvokingFunc()
        {
            var calls = 0;
            var cached = _proxy.Get<int>(k => { calls++; return 42; }, _options);

            var first = cached("key");
            var second = cached("key");

            Assert.Equal(42, first);
            Assert.Equal(42, second);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Get_DifferentKeys_InvokeFuncOncePerKey()
        {
            var calls = 0;
            var cached = _proxy.Get<string>(k => { calls++; return k.ToUpperInvariant(); }, _options);

            Assert.Equal("A", cached("a"));
            Assert.Equal("B", cached("b"));
            Assert.Equal("A", cached("a")); // cache hit

            Assert.Equal(2, calls);
        }

        [Fact]
        public void Get_CacheHit_ReturnsSameInstance()
        {
            Payload produced = null;
            var cached = _proxy.Get<Payload>(k => produced = new Payload { Name = "x", Value = 7 }, _options);

            var first = cached("key");   // returns the produced instance
            var second = cached("key");  // served from memory as-is

            Assert.Same(produced, first);
            Assert.Same(produced, second); // no serialization -> same reference
        }

        // ── Asynchronous memoization (Task<T>) ───────────────────────────────────

        [Fact]
        public async Task Get_AsyncTaskFunc_MemoizesResult()
        {
            var calls = 0;
            var cached = _proxy.Get<Task<int>>(async k => { calls++; await Task.Yield(); return 99; }, _options);

            var first = await cached("key");
            var second = await cached("key");

            Assert.Equal(99, first);
            Assert.Equal(99, second);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task Get_AsyncTaskFunc_CacheHit_ReturnsSameInstance()
        {
            Payload produced = null;
            var cached = _proxy.Get<Task<Payload>>(async k =>
            {
                await Task.Yield();
                return produced = new Payload { Name = "x", Value = 7 };
            }, _options);

            var first = await cached("key");
            var second = await cached("key");

            Assert.Same(produced, first);
            Assert.Same(produced, second); // no serialization -> same reference
        }

        [Fact]
        public async Task Get_AsyncTaskFunc_DifferentKeys_InvokeFuncOncePerKey()
        {
            var calls = 0;
            var cached = _proxy.Get<Task<int>>(async k => { calls++; await Task.Yield(); return k.Length; }, _options);

            Assert.Equal(1, await cached("a"));
            Assert.Equal(2, await cached("bb"));
            Assert.Equal(1, await cached("a")); // cache hit

            Assert.Equal(2, calls);
        }

        // ── Asynchronous memoization (ValueTask<T>) ──────────────────────────────

        [Fact]
        public async Task Get_AsyncValueTaskFunc_MemoizesResult()
        {
            var calls = 0;
            var cached = _proxy.Get<ValueTask<string>>(async k => { calls++; await Task.Yield(); return "v"; }, _options);

            var first = await cached("key");
            var second = await cached("key");

            Assert.Equal("v", first);
            Assert.Equal("v", second);
            Assert.Equal(1, calls);
        }
    }
}
