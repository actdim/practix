using Autofac;
using ActDim.Practix.Abstractions.DataAccess;
using ActDim.Practix.Abstractions.Json;
using System.Data.Common;

namespace ActDim.Practix.DataAccess.EntityMapping.Fetch
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