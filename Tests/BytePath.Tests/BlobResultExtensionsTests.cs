using System;
using Xunit;

namespace ActDim.BytePath.Tests
{
    public class BlobResultExtensionsTests
    {
        [Fact]
        public void EnsureSuccess_NullBlobResult_ThrowsArgumentNullException()
        {
            BlobResult? result = null;

            Assert.Throws<ArgumentNullException>(() => result!.EnsureSuccess());
        }

        [Fact]
        public void EnsureSuccess_FailedErrorCode_ThrowsInvalidOperationException()
        {
            var result = new BlobResult(BlobErrorCode.KeyNotFound);

            var ex = Assert.Throws<InvalidOperationException>(() => result.EnsureSuccess());
            Assert.Contains("BLOB operation failed", ex.Message);
            Assert.Contains("KeyNotFound", ex.Message);
        }

        [Fact]
        public void EnsureSuccess_SuccessErrorCodeButNullRecord_ThrowsInvalidOperationException()
        {
            var result = new BlobResult(BlobErrorCode.None, record: null);

            var ex = Assert.Throws<InvalidOperationException>(() => result.EnsureSuccess());
            Assert.Contains("record is missing", ex.Message);
        }

        [Fact]
        public void EnsureSuccess_ValidResult_ReturnsSameInstance()
        {
            using var record = new BlobRecord { Key = "my-key", Hash = "hash-123", Size = 100 };
            var result = new BlobResult(BlobErrorCode.None, record);

            var validated = result.EnsureSuccess();

            Assert.Same(result, validated);
        }

        [Fact]
        public void EnsureRecord_ValidResult_ReturnsRecord()
        {
            using var record = new BlobRecord { Key = "my-key", Hash = "hash-123", Size = 100 };
            var result = new BlobResult(BlobErrorCode.None, record);

            var extractedRecord = result.EnsureRecord();

            Assert.Same(record, extractedRecord);
        }
    }
}
