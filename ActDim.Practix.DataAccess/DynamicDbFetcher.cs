using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using System.Data.Common;
using System.Dynamic;

namespace OrthoBits.DataAccess
{
    public class DynamicDbFetcher<T> : IDbFetcher<T>
    {
        public IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            var list = new List<object>();
            while (reader.Read())
            {
                list.Add(MapDatabaseRow(reader));
            }
            return (IList<T>)list;
        }


        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            return await FetchAsync(reader, mapper, serializer, CancellationToken.None);
        }

        public async Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            var list = new List<object>();
            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                list.Add(MapDatabaseRow(reader));
            }
            return (IList<T>)list;
        }

        private static ExpandoObject MapDatabaseRow(DbDataReader reader)
        {
            var expando = new ExpandoObject();
            IDictionary<string, object> dict = expando;
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                dict[reader.GetName(i)] = value;
            }

            return expando;
        }
    }
}
