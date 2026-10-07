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
        public void MinOrDefault_WithNaN_PropagatesNaNByDefault()
        {
            var leadingNan = new[] { double.NaN, 10.0, 5.0 };
            var middleNan = new[] { 10.0, double.NaN, 5.0 };
            var trailingNan = new[] { 10.0, 5.0, double.NaN };
            var onlyNan = new[] { double.NaN };

            Assert.True(double.IsNaN(leadingNan.MinOrDefault(x => x, 0.0)));
            Assert.True(double.IsNaN(middleNan.MinOrDefault(x => x, 0.0)));
            Assert.True(double.IsNaN(trailingNan.MinOrDefault(x => x, 0.0)));
            Assert.True(double.IsNaN(onlyNan.MinOrDefault(x => x, 0.0)));
        }

        [Fact]
        public void MinOrDefault_WithIgnoreNaN_IgnoresNaNAndComputesMinimum()
        {
            var leadingNan = new[] { double.NaN, 10.0, 5.0 };
            var middleNan = new[] { 10.0, double.NaN, 5.0 };
            var trailingNan = new[] { 10.0, 5.0, double.NaN };
            var onlyNan = new[] { double.NaN, double.NaN };

            Assert.Equal(5.0, leadingNan.MinOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(5.0, middleNan.MinOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(5.0, trailingNan.MinOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(42.0, onlyNan.MinOrDefault(x => x, 42.0, ignoreNaN: true));
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
        public void MaxOrDefault_WithNaN_MatchesLinqSemantics()
        {
            var leadingNan = new[] { double.NaN, 2.0, 8.0 };
            var middleNan = new[] { 2.0, double.NaN, 8.0 };
            var trailingNan = new[] { 2.0, 8.0, double.NaN };
            var singleNan = new[] { double.NaN };
            var allNan = new[] { double.NaN, double.NaN };

            // In LINQ total ordering, NaN is ordered smaller than any real number.
            // Max returns the maximum real number if any exists, and returns NaN only when all elements are NaN.
            Assert.Equal(8.0, leadingNan.MaxOrDefault(x => x, 0.0));
            Assert.Equal(8.0, middleNan.MaxOrDefault(x => x, 0.0));
            Assert.Equal(8.0, trailingNan.MaxOrDefault(x => x, 0.0));
            Assert.True(double.IsNaN(singleNan.MaxOrDefault(x => x, 0.0)));
            Assert.True(double.IsNaN(allNan.MaxOrDefault(x => x, 0.0)));
        }

        [Fact]
        public void MaxOrDefault_WithIgnoreNaN_IgnoresNaNAndComputesMaximum()
        {
            var leadingNan = new[] { double.NaN, 2.0, 8.0 };
            var middleNan = new[] { 2.0, double.NaN, 8.0 };
            var trailingNan = new[] { 2.0, 8.0, double.NaN };
            var onlyNan = new[] { double.NaN, double.NaN };

            Assert.Equal(8.0, leadingNan.MaxOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(8.0, middleNan.MaxOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(8.0, trailingNan.MaxOrDefault(x => x, 0.0, ignoreNaN: true));
            Assert.Equal(-1.0, onlyNan.MaxOrDefault(x => x, -1.0, ignoreNaN: true));
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
