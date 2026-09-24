using System;
using System.Collections.Generic;
using ActDim.Practix.Collections.Specialized;
using Xunit;

namespace ActDim.Practix.Common.Tests.Collections.Specialized
{
    public class StaticBiMapTests
    {
        [Fact]
        public void ForwardAndReverseLookups_Work()
        {
            var items = new[]
            {
                new KeyValuePair<string,int>("foo",1),
                new KeyValuePair<string,int>("bar",2)
            };
            int forwardFallback(string key) => -1;
            string reverseFallback(int value) => "missing";

            var bimap = StaticMap.CreateBiMap(items, forwardFallback, reverseFallback);

            Assert.Equal(1, bimap["foo"]);
            Assert.Equal(2, bimap["bar"]);
            Assert.Equal("missing", bimap[99]);
            Assert.Equal("foo", bimap[1]);
            Assert.Equal("bar", bimap[2]);
            Assert.Equal(-1, bimap["baz"]);
        }

        [Fact]
        public void GeneratedImplementation_WorkWithStringKeys()
        {
            var items = new[]
            {
                new KeyValuePair<string, int>("x", 1),
                new KeyValuePair<string, int>("y", 2)
            };
            int forwardFallback(string key) => -1;
            string reverseFallback(int value) => "?";

            var bimap = StaticMap.CreateBiMap<string, int>(items, forwardFallback, (Func<int, string>)reverseFallback, StaticMapLookup.Generated);

            Assert.Equal(1, bimap["x"]);
            Assert.Equal(2, bimap["y"]);
            Assert.Equal(-1, bimap["z"]);
            Assert.Equal("x", bimap[1]);
            Assert.Equal("y", bimap[2]);
            Assert.Equal("?", bimap[99]);
        }

        [Fact]
        public void GeneratedImplementation_WorksWithNonStringKeys()
        {
            var items = new[]
            {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two")
            };

            string forwardFallback(int key)
            {
                return "none";
            }

            int reverseFallback(string value)
            {
                return 0;
            }

            var bimap = StaticMap.CreateBiMap<int, string>(
                items,
                forwardFallback,
                reverseFallback,
                StaticMapLookup.Generated);

            Assert.Equal("one", bimap[1]);
            Assert.Equal("two", bimap[2]);
            Assert.Equal("none", bimap[99]);

            Assert.Equal(1, bimap["one"]);
            Assert.Equal(2, bimap["two"]);
            Assert.Equal(0, bimap["unknown"]);
        }

        [Fact]
        public void Immutability()
        {
            var list = new List<KeyValuePair<string, int>>();
            list.Add(new KeyValuePair<string, int>("a", 1));
            int forwardFallback(string key) => -1;
            string reverseFallback(int value) => "missing";

            var bimap = StaticMap.CreateBiMap(list, forwardFallback, reverseFallback);

            // mutate source after creation
            list.Add(new KeyValuePair<string, int>("b", 2));
            Assert.Equal(-1, bimap["b"]);
        }
    }
}
