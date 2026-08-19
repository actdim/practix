using ActDim.Practix.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Extensions
{
    public class FuncExtensionsTests
    {
        [Fact]
        public void Memoize_CachesFunctionResults()
        {
            var callCount = 0;
            Func<int, int> square = x =>
            {
                callCount++;
                return x * x;
            };

            var memoized = square.Memoize();

            Assert.Equal(16, memoized(4));
            Assert.Equal(16, memoized(4));
            Assert.Equal(25, memoized(5));
            Assert.Equal(25, memoized(5));
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task Memoize_ThreadSafeAndExactlyOncePerKey()
        {
            var callCount = 0;
            Func<string, string> slowTransform = key =>
            {
                Thread.Sleep(10);
                Interlocked.Increment(ref callCount);
                return "Processed_" + key;
            };

            var memoized = slowTransform.Memoize();

            var tasks = Enumerable.Range(0, 50)
                .Select(_ => Task.Run(() => memoized("sampleKey")))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            Assert.All(results, res => Assert.Equal("Processed_sampleKey", res));
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Memoize_CustomComparer_WorksCorrectly()
        {
            var callCount = 0;
            Func<string, int> stringLength = s =>
            {
                callCount++;
                return s.Length;
            };

            var memoized = stringLength.Memoize(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(4, memoized("TEST"));
            Assert.Equal(4, memoized("test"));
            Assert.Equal(4, memoized("Test"));
            Assert.Equal(1, callCount);
        }
    }
}
