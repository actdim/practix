using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrthoBits.DataAccess
{
    internal class LambdaDbFetcherAsync<T> : IDbFetcher<T>
    {
        private readonly Func<DbDataReader, Task<T>> _readerAsync;

        public LambdaDbFetcherAsync(Func<DbDataReader, Task<T>> readerAsync)
        {
            _readerAsync = readerAsync;
        }

        public IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return await FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = await _readerAsync(reader);
                list.Add(item);
            }
            return list;
        }
    }
}