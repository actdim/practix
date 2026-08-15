using System;
using System.Collections.Generic;
using ActDim.Practix.Common.Runtime;
using Xunit;

namespace ActDim.Practix.Common.Tests.Runtime
{
    public class StaticMapTests
    {
        [Fact]
        public void FrozenImplementation_ReturnsValuesAndFallback()
        {
            var items = new[]
            {
                new KeyValuePair<string,int>("a",1),
                new KeyValuePair<string,int>("b",2)
            };
            int fallback(string key) => -1;
            var map = StaticMap.Create(items, fallback, StaticMapLookup.Frozen);

            Assert.Equal(1, map["a"]);
            Assert.Equal(2, map["b"]);
            Assert.Equal(-1, map["c"]);
        }

        [Fact]
        public void GeneratedStringImplementation_ReturnsValuesAndFallback()
        {
            var items = new[]
            {
                new KeyValuePair<string,int>("x",10),
                new KeyValuePair<string,int>("y",20)
            };
            int fallback(string key) => 0;
            var map = StaticMap.Create(items, fallback, StaticMapLookup.Generated);

            Assert.Equal(10, map["x"]);
            Assert.Equal(20, map["y"]);
            Assert.Equal(0, map["z"]);
        }

        [Fact]
        public void GeneratedNonStringImplementation_ReturnsValuesAndFallback()
        {
            var items = new[]
            {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two")
            };
            string fallback(int key) => "none";
            var map = StaticMap.Create(items, fallback, StaticMapLookup.Generated);

            Assert.Equal("one", map[1]);
            Assert.Equal("two", map[2]);
            Assert.Equal("none", map[3]);
        }

        [Fact]
        public void GeneratedGuidImplementation_ReturnsValuesAndFallback()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var items = new[]
            {
                new KeyValuePair<Guid, string>(id1, "first"),
                new KeyValuePair<Guid, string>(id2, "second")
            };
            string fallback(Guid key) => "unknown";
            var map = StaticMap.Create(items, fallback, StaticMapLookup.Generated);

            Assert.Equal("first", map[id1]);
            Assert.Equal("second", map[id2]);
            Assert.Equal("unknown", map[id3]);
        }

        [Fact]
        public void MapImmutability()
        {
            var list = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("p", 5)
            };
            int fallback(string key) => -1;
            var map = StaticMap.Create(list, fallback);

            // mutate source after creation
            list.Add(new KeyValuePair<string,int>("q",6));
            Assert.Equal(-1, map["q"]);
        }
    }
}
