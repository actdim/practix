using Autofac;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using OrthoBits.DataAccess.EntityMapping.Fetch;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    public class CommonDbFetcher<T> : IDbFetcher<T>
    {
        private readonly ILifetimeScope _scope;

        private readonly DbProviderType _providerType;

        public CommonDbFetcher(DbProviderType providerType, ILifetimeScope scope)
        {
            _providerType = providerType;
            _scope = scope;
        }

        public IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            if (!reader.HasRows)
            {
                return new List<T>();
            }
            
            List<T> list = new List<T>();
            var fetcher = EntityFetcher<T>.GetFetcher(reader, _providerType, _scope);
            while (reader.Read())
            {
                list.Add(fetcher.Fetch(reader, serializer));
            }
            return list;
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return await FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper,
            IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            if (!reader.HasRows)
            {
                return new List<T>();
            }
            var list = new List<T>();
            var fetcher = EntityFetcher<T>.GetFetcher(reader, _providerType, _scope);
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                list.Add(fetcher.Fetch(reader, serializer));
            }
            return list;
        }
    }
}
