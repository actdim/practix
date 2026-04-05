
using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    internal class LambdaDbFetcher<T> : IDbFetcher<T>
    {
        private readonly Func<DbDataReader, T> _reader;

        public LambdaDbFetcher(Func<DbDataReader, T> reader)
        {
            _reader = reader;
        }

        public IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                var item = _reader(reader);
                list.Add(item);
            }
            return list;
        }

        public Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}