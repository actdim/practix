using ActDim.Practix.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Collections
{
    public class ConcurrentFactoryDictionaryTests
    {
        [Fact]
        public void GetOrCreateValue_CreatesValueOnFirstAccess()
        {
            int factoryCallCount = 0;
            var dict = new ConcurrentFactoryDictionary<string, string>(key =>
            {
                factoryCallCount++;
                return "Value_" + key;
            });

            var val1 = dict.GetOrCreateValue("k1");
            var val2 = dict.GetOrCreateValue("k1");

            Assert.Equal("Value_k1", val1);
            Assert.Equal("Value_k1", val2);
            Assert.Equal(1, factoryCallCount);
        }

        [Fact]
        public async Task GetOrCreateValue_ThreadSafeAndExactlyOncePerKey()
        {
            int factoryCallCount = 0;
            var dict = new ConcurrentFactoryDictionary<int, string>(key =>
            {
                Task.Delay(10).Wait();
                System.Threading.Interlocked.Increment(ref factoryCallCount);
                return "Val_" + key;
            });

            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => dict.GetOrCreateValue(42)))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            Assert.All(results, res => Assert.Equal("Val_42", res));
            Assert.Equal(1, factoryCallCount);
        }

        [Fact]
        public void DictionaryOperations_WorkAsExpected()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>(key => key.Length);

            dict.GetOrCreateValue("a");
            dict.GetOrCreateValue("bb");
            dict.GetOrCreateValue("ccc");

            Assert.Equal(3, dict.Count);
            Assert.True(dict.ContainsKey("bb"));
            Assert.False(dict.ContainsKey("dddd"));

            Assert.Equal(2, dict["bb"]);

            Assert.True(dict.TryGetValue("ccc", out var cccVal));
            Assert.Equal(3, cccVal);

            Assert.False(dict.TryGetValue("missing", out var missingVal));
            Assert.Equal(0, missingVal);

            var keys = dict.Keys.ToList();
            Assert.Contains("a", keys);
            Assert.Contains("bb", keys);
            Assert.Contains("ccc", keys);

            var values = dict.Values.ToList();
            Assert.Contains(1, values);
            Assert.Contains(2, values);
            Assert.Contains(3, values);
        }

        [Fact]
        public void TryRemove_RemovesEntry()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>(key => key.Length);

            dict.GetOrCreateValue("test");
            Assert.True(dict.ContainsKey("test"));

            Assert.True(dict.TryRemove("test", out var removedVal));
            Assert.Equal(4, removedVal);
            Assert.False(dict.ContainsKey("test"));

            Assert.False(dict.TryRemove("test", out _));
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var dict = new ConcurrentFactoryDictionary<int, int>(key => key * 10);
            dict.GetOrCreateValue(1);
            dict.GetOrCreateValue(2);

            Assert.Equal(2, dict.Count);
            dict.Clear();
            Assert.Empty(dict);
        }

        [Fact]
        public void EnumerableInterface_WorksWithLinq()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>(key => "V_" + key);
            dict.GetOrCreateValue("x");
            dict.GetOrCreateValue("y");

            var list = dict.ToList();
            Assert.Equal(2, list.Count);
            Assert.Contains(list, kvp => kvp.Key == "x" && kvp.Value == "V_x");
            Assert.Contains(list, kvp => kvp.Key == "y" && kvp.Value == "V_y");
        }

        [Fact]
        public void FailedFactory_AllowsRetryOnNextCall()
        {
            bool shouldFail = true;
            var dict = new ConcurrentFactoryDictionary<string, string>(key =>
            {
                if (shouldFail)
                {
                    throw new InvalidOperationException("Factory failed");
                }
                return "Success";
            });

            Assert.Throws<InvalidOperationException>(() => dict.GetOrCreateValue("k1"));

            shouldFail = false;
            var res = dict.GetOrCreateValue("k1");
            Assert.Equal("Success", res);
        }
    }
}
