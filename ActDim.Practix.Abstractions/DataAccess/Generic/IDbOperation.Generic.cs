using System.Data.Common;

namespace ActDim.Practix.Abstractions.DataAccess.Generic
{
    public interface IDbOperation<T> : IDbOperation
    {
        IDbFetcher<T> Fetcher { get; }
    }
}
