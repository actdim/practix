using ActDim.Practix.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActDim.Practix.Common.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void Partition_SplitsSequenceIntoChunks()
        {
            var source = Enumerable.Range(1, 10);
            var partitions = source.Partition(3).ToList();

            Assert.Equal(4, partitions.Count);
            Assert.Equal([1, 2, 3], partitions[0]);
            Assert.Equal([4, 5, 6], partitions[1]);
            Assert.Equal([7, 8, 9], partitions[2]);
            Assert.Equal([10], partitions[3]);
        }

        [Fact]
        public void Partition_EmptySequence_ReturnsEmpty()
        {
            var source = Enumerable.Empty<int>();
            var partitions = source.Partition(5).ToList();

            Assert.Empty(partitions);
        }

        [Fact]
        public void IsNullOrEmpty_VariousCollections_BehavesCorrectly()
        {
            IEnumerable<int> nullSeq = null;
            Assert.True(nullSeq.IsNullOrEmpty());

            var emptyList = new List<int>();
            Assert.True(emptyList.IsNullOrEmpty());

            var emptyArray = Array.Empty<string>();
            Assert.True(emptyArray.IsNullOrEmpty());

            var nonEmptyList = new List<int> { 1, 2 };
            Assert.False(nonEmptyList.IsNullOrEmpty());
        }

        [Fact]
        public void MinOrDefault_ComputesCorrectMinOrFallback()
        {
            var empty = Enumerable.Empty<string>();
            Assert.Equal(99.0, empty.MinOrDefault(s => s.Length, 99.0));

            var items = new[] { "apple", "cat", "banana" };
            Assert.Equal(3.0, items.MinOrDefault(s => s.Length, 0.0));
        }

        [Fact]
        public void MaxOrDefault_ComputesCorrectMaxOrFallback()
        {
            var empty = Enumerable.Empty<string>();
            Assert.Equal(-1.0, empty.MaxOrDefault(s => s.Length, -1.0));

            var items = new[] { "apple", "cat", "banana" };
            Assert.Equal(6.0, items.MaxOrDefault(s => s.Length, 0.0));
        }

        [Fact]
        public void EstimateCount_EvaluatesCorrectly()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            Assert.True(items.EstimateCount(3, x => x % 2 != 0));
            Assert.False(items.EstimateCount(4, x => x % 2 != 0));
        }
    }
}
