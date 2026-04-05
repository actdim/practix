using Autofac;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using OrthoBits.DataAccess.EntityMapping.Fetch;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrthoBits.DataAccess
{
    /// <summary>
    /// ExtendedFetcher
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class TupleFetcher<T1, T2> : IDbFetcher<(T1, T2)>
    {
        private readonly DbProviderType _providerType;
        private readonly ILifetimeScope _scope;

        public TupleFetcher(DbProviderType dbType, ILifetimeScope scope)
        {
            _providerType = dbType;
            _scope = scope;
        }

        public IList<(T1, T2)> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return FetchAsync(reader, mapper, serializer, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<IList<(T1, T2)>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<(T1, T2)>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            if (!reader.HasRows)
            {
                return new List<(T1, T2)>();
            }
            var result = new List<(T1, T2)>();
            var fetcher1 = EntityFetcher<T1>.GetFetcher(reader, _providerType, _scope);
            var fetcher2 = EntityFetcher<T2>.GetFetcher(reader, _providerType, _scope);
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item1 = fetcher1.Fetch(reader, serializer);
                var item2 = fetcher2.Fetch(reader, serializer);
                result.Add((item1, item2));
            }
            return result;
        }
    }

    /// <summary>
    /// ExtendedFetcher
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    public class TupleFetcher<T1, T2, T3> : IDbFetcher<(T1, T2, T3)>
    {
        private readonly DbProviderType _providerType;
        private readonly ILifetimeScope _scope;

        public TupleFetcher(DbProviderType providerType, ILifetimeScope scope)
        {
            _providerType = providerType;
            _scope = scope;
        }

        public IList<(T1, T2, T3)> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return FetchAsync(reader, mapper, serializer, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<IList<(T1, T2, T3)>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<(T1, T2, T3)>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            if (!reader.HasRows)
            {
                return new List<(T1, T2, T3)>();
            }
            var result = new List<(T1, T2, T3)>();
            var fetcher1 = EntityFetcher<T1>.GetFetcher(reader, _providerType, _scope);
            var fetcher2 = EntityFetcher<T2>.GetFetcher(reader, _providerType, _scope);
            var fetcher3 = EntityFetcher<T3>.GetFetcher(reader, _providerType, _scope);
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item1 = fetcher1.Fetch(reader, serializer);
                var item2 = fetcher2.Fetch(reader, serializer);
                var item3 = fetcher3.Fetch(reader, serializer);
                result.Add((item1, item2, item3));
            }
            return result;
        }
    }
}