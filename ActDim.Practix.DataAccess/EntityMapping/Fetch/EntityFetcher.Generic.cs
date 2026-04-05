using Autofac;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.Json;
using System.Data.Common;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    public class EntityFetcher<T> : EntityFetcher
    {
        internal EntityFetcher(DbDataReader dataReader, EntityTable entityTable, ILifetimeScope scope)
            : base(dataReader, entityTable, new FetcherEntityFactory(scope))
        {
        }

        public static EntityFetcher<T> GetFetcher(DbDataReader dataReader, DbProviderType providerType, ILifetimeScope scope)
        {
            var entityType = typeof(T);
            var table = HelperCaches.GetEntityTable(entityType, providerType);
            return new EntityFetcher<T>(dataReader, table, scope);
        }

        public new T Fetch(DbDataReader dataReader, IJsonSerializer stdSerializer)
        {
            return (T)base.Fetch(dataReader, stdSerializer);
        }
    }
}