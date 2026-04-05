using Autofac;
using OrthoBits.Abstractions.DataAccess;
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
    public class SinglePrimitiveDatabaseFetcher<T, TResultSerializer> : SinglePrimitiveDatabaseFetcher<T>
        where TResultSerializer : IColumnSerializer
    {
        private readonly IColumnSerializer _serializer;

        public SinglePrimitiveDatabaseFetcher(ILifetimeScope scope) : base()
        {
            _serializer = HelperCaches.GetSerializer(typeof(TResultSerializer));
        }

        protected override T ReadFieldValue(DbDataReader reader)
        {
            var value = reader.GetValue(0);
            return (T)_serializer.Deserialize(value);
        }
    }

    public class SinglePrimitiveDatabaseFetcher<T> : IDbFetcher<T>
    {
        protected virtual T ReadFieldValue(DbDataReader reader)
        {
            return reader.GetFieldValue<T>(0);
        }

        public IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            var list = new List<T>();
            if (!reader.HasRows)
            {
                return list;
            }
            var type = typeof(T);
            var nullable = Nullable.GetUnderlyingType(type) != null || type == typeof(string);
            while (reader.Read())
            {
                if (nullable && reader.IsDBNull(0))
                {
                    list.Add((T)(object)null);
                }
                list.Add(ReadFieldValue(reader));
            }
            return list;
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return await FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            if (!reader.HasRows)
            {
                return list;
            }
            var type = typeof(T);
            var nullable = Nullable.GetUnderlyingType(type) != null || type == typeof(string);
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (nullable && reader.IsDBNull(0))
                {
                    list.Add((T)(object)null);
                }
                list.Add(ReadFieldValue(reader));
            }
            return list;
        }
    }
}