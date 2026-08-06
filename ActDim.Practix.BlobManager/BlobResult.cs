using System;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public sealed class BlobResult : IAsyncDisposable, IDisposable
    {
        internal BlobResult(BlobErrorCode errorCode, BlobRecord record = null, bool isNew = false)
        {
            ErrorCode = errorCode;
            Record = record;
            IsNew = isNew;
        }

        public BlobErrorCode ErrorCode { get; }
        public BlobRecord Record { get; }
        public bool IsSuccess => ErrorCode == BlobErrorCode.None;
        public bool IsNew { get; internal set; }

        public void Deconstruct(out BlobErrorCode errorCode, out BlobRecord record)
            => (errorCode, record) = (ErrorCode, Record);

        public void Dispose() => Record?.Dispose();

        public ValueTask DisposeAsync() =>
            Record?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
