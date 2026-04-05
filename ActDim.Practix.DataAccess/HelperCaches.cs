using OrthoBits.DataAccess.EntityMapping.Fetch;
using FastMember;
using System.Collections.Concurrent;
using OrthoBits.Abstractions.DataAccess;
using System.Data.Common;
using OrthoBits.DataAccess.Attributes;
using OrthoBits.Abstractions.DataAccess;

namespace OrthoBits.DataAccess
{
    /// <summary>
    /// Helper
    /// </summary>
	internal static class HelperCaches
    {
        private static ConcurrentDictionary<Type, EntityTable> TableCache;
        private static ConcurrentDictionary<Type, IColumnSerializer> SerializerCache;
        private static ConcurrentDictionary<DbProviderType, ISqlDialect> ProjectorCache;
        private static ConcurrentDictionary<Type, IPropertyWriter> PropertyWriterCache;
        private static ConcurrentDictionary<Type, IPropertyActivator> PropertyActivatorCache;

        static HelperCaches()
        {
            Reset();
        }

        public static void Reset()
        {
            TableCache = new ConcurrentDictionary<Type, EntityTable>();
            SerializerCache = new ConcurrentDictionary<Type, IColumnSerializer>();
            ProjectorCache = new ConcurrentDictionary<DbProviderType, ISqlDialect>();
            PropertyWriterCache = new ConcurrentDictionary<Type, IPropertyWriter>();
            PropertyActivatorCache = new ConcurrentDictionary<Type, IPropertyActivator>();
        }

        public static EntityTable GetEntityTable(Type type, DbProviderType dbProviderType)
        {
            return TableCache.GetOrAdd(type, key => new EntityTable(TypeAccessor.Create(key, true), key, dbProviderType));
        }

        public static EntityTable GetEntityTable(Type type)
        {
            return TableCache.GetOrAdd(type, key => new EntityTable(TypeAccessor.Create(key, true), key));
        }

        public static IColumnSerializer GetSerializer(Type type)
        {
            return SerializerCache.GetOrAdd(type, key => (IColumnSerializer)TypeAccessor.Create(key, true).CreateNew());
        }

        public static IPropertyWriter GetPropertyWriter(Type type)
        {
            return PropertyWriterCache.GetOrAdd(type, key => (IPropertyWriter)TypeAccessor.Create(key, true).CreateNew());
        }

        public static IPropertyActivator GetPropertyActivator(Type type)
        {
            return PropertyActivatorCache.GetOrAdd(type,
                key => (IPropertyActivator)TypeAccessor.Create(key).CreateNew());
        }

        private static DbConnectionStringBuilder ConnectionStringBuilder = new DbConnectionStringBuilder();
        private static DbProviderType GetProviderType(string connString)
        {
            // DbProviderType result = default;
            // TODO: implement
            // ConnectionStringBuilder...
            throw new NotImplementedException();            
        }

        public static ISqlDialect GetDialect(string connString)
        {
            return GetDialect(GetProviderType(connString));
        }

        public static ISqlDialect GetDialect(DbConnection connection)
        {
            return GetDialect(connection.ConnectionString);
        }

        public static ISqlDialect GetDialect(DbProviderType providerType)
        {            
            return ProjectorCache.GetOrAdd(providerType, key =>
            {
                var type = typeof(DbProviderType);
                if (type.GetMember(providerType.ToString()).FirstOrDefault()?
                    .GetCustomAttributes(typeof(ConventionProjectorAttribute), false)?
                    .FirstOrDefault() is not ConventionProjectorAttribute attribute)
                {
                    throw new InvalidOperationException($"{nameof(ConventionProjectorAttribute)} expected for {providerType} database type"); // can't determine projector
                }
                return (ISqlDialect)TypeAccessor.Create(attribute.ConventionProjector).CreateNew();
            });
        }

        public static void RegisterProjector(DbProviderType providerType, ISqlDialect projector)
        {
            ProjectorCache.TryAdd(providerType, projector);
        }
    }
}
