using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Sql;
using OrthoBits.DataAccess.EntityMapping.Fetch;
using OrthoBits.DataAccess.Extensions;
using System.Data.Common;
using System.Linq.Expressions;

namespace OrthoBits.DataAccess.Sql
{
    public static class SqlGeneratorExtensions
    {
        public static IDbOperation Insert<TTable>(this ISqlWithTable<TTable> sql, TTable entity, IEnumerable<string> include = null, DbProviderType providerType = default)
            where TTable : class
        {
            var changedEntity = entity.ToChangedEntity(include.ToArray());
            return sql.Insert(changedEntity, include, providerType);
        }

        public static IDbOperation Insert<TTable>(this ISqlWithTable<TTable> sql, ChangedEntity<TTable> entityChangeSet, IEnumerable<string> include = null, DbProviderType providerType = default)
            where TTable : class
        {
            var generatorInstance = (SqlGenerator)sql;
            IEnumerable<string> includeFields = entityChangeSet.ModifiedFields;
            if (include != null)
            {
                var set = new HashSet<string>(includeFields, StringComparer.InvariantCultureIgnoreCase);
                set.UnionWith(include);
                includeFields = set;
            }

            if (!includeFields.Any())
            {
                throw new InvalidOperationException("There are no modified fields. Nothing to insert");
            }

            var entity = entityChangeSet.Entity;
            var entityTable = providerType == default ? HelperCaches.GetEntityTable(typeof(TTable)) : HelperCaches.GetEntityTable(typeof(TTable), providerType);
            var mapper = new EntityToDbMapper(entityTable, generatorInstance.JsonSerializer);
            var values = mapper.MapToDbValues(entity, includeFields);
            var projector = HelperCaches.GetDialect(entityTable.ProviderType);
            return new CommonDbOperation(entityTable.ProviderType, $"INSERT INTO {entityTable.TableName} " + $"({string.Join(", ", values.Select(x => x.ColumnName))}) VALUES " + $"({string.Join(", ", values.Select(x => projector.ParameterNamePrefix + x.PropertyName))})", values.Select(x => new DbParam()
            { ParameterName = x.PropertyName, Value = x.Value }).Cast<DbParameter>().ToArray());
        }

        public static IDbOperation Delete<TParams, TTable>(this ISqlWithParamsAndTable<TParams, TTable> sql, Expression<Func<TParams, TTable, string>> conditions, DbProviderType providerType = default)
        {
            var condition = sql.CreateOperation(conditions);
            return sql.CreateOperation((param, table) => $"DELETE FROM {table} WHERE {condition}", providerType);
        }

        [Obsolete("Replaced by TryUpdate")]
        public static IDbOperation Update<TTable>(this ISqlWithTable<TTable> sql, ChangedEntity<TTable> entityChangeSet, Expression<Func<TTable, TTable, string>> conditions, IEnumerable<string> exclude = null, DbProviderType providerType = default)
            where TTable : class
        {
            var generator = (SqlGenerator)sql;
            IEnumerable<string> includeFields = entityChangeSet.ModifiedFields;
            if (exclude != null)
            {
                var set = new HashSet<string>(includeFields, StringComparer.InvariantCultureIgnoreCase);
                set.ExceptWith(exclude);
                includeFields = set;
            }

            if (!includeFields.Any())
            {
                throw new InvalidOperationException("There are no modified field in change set");
            }

            var entity = entityChangeSet.Entity;
            var entityTable = providerType == default ? HelperCaches.GetEntityTable(typeof(TTable)) : HelperCaches.GetEntityTable(typeof(TTable), providerType);
            var mapper = new EntityToDbMapper(entityTable, generator.JsonSerializer);
            var values = mapper.MapToDbValues(entity, includeFields);
            var projector = HelperCaches.GetDialect(entityTable.ProviderType);
            var update = new CommonDbOperation(entityTable.ProviderType, $"UPDATE {entityTable.TableName} SET " + $"{string.Join(", ", values.Select(v => $"{v.ColumnName} = {projector.ParameterNamePrefix}{v.PropertyName}"))}", values.Select(x => new DbParam()
            { ParameterName = x.PropertyName, Value = x.Value }).Cast<DbParameter>().ToArray());
            var condition = generator.Table<TTable>().Params(entity).CreateOperation(conditions);
            return generator.Table<TTable>().CreateOperation(table => $"{update} WHERE {condition}");
        }

        public static bool TryUpdate<TTable>(this ISqlWithTable<TTable> sql, ChangedEntity<TTable> entityChangeSet, Expression<Func<TTable, TTable, string>> conditions, out IDbOperation result, IEnumerable<string> exclude = null, DbProviderType providerType = default)
            where TTable : class
        {
            result = null;
            var generator = (SqlGenerator)sql;
            IEnumerable<string> includeFields = entityChangeSet.ModifiedFields;
            if (exclude != null)
            {
                var set = new HashSet<string>(includeFields, StringComparer.InvariantCultureIgnoreCase);
                set.ExceptWith(exclude);
                includeFields = set;
            }

            if (!includeFields.Any())
            {
                return false;
            }

            var entity = entityChangeSet.Entity;
            var entityTable = providerType == default ? HelperCaches.GetEntityTable(typeof(TTable)) : HelperCaches.GetEntityTable(typeof(TTable), providerType);
            var mapper = new EntityToDbMapper(entityTable, generator.JsonSerializer);
            var values = mapper.MapToDbValues(entity, includeFields);
            var projector = HelperCaches.GetDialect(entityTable.ProviderType);
            var update = new CommonDbOperation(entityTable.ProviderType, $"UPDATE {entityTable.TableName} SET " + $"{string.Join(", ", values.Select(v => $"{v.ColumnName} = {projector.ParameterNamePrefix}{v.PropertyName}"))}", values.Select(x => new DbParam()
            { ParameterName = x.PropertyName, Value = x.Value }).Cast<DbParameter>().ToArray());
            var condition = generator.Table<TTable>().Params(entity).CreateOperation(conditions);
            result = generator.Table<TTable>().CreateOperation(table => $"{update} WHERE {condition}");
            return true;
        }

        /// <summary>
        /// Removes all unrecognized modified fields including Ignored
        /// </summary>
        /// <typeparam name = "TTable">entity table</typeparam>
        /// <param name = "sql"></param>
        /// <param name = "entityChangeSet">entity change set</param>
        /// <param name = "forceExclude">force exclude specified fields</param>
        /// <param name = "providerType">type of database</param>
        /// <returns></returns>
        public static ChangedEntity<TTable> FilterModifiedFields<TTable>(this ISqlWithTable<TTable> sql, ChangedEntity<TTable> entityChangeSet, IEnumerable<string> forceExclude = null, DbProviderType providerType = default)
            where TTable : class
        {
            var entityTable = providerType == default ? HelperCaches.GetEntityTable(typeof(TTable)) : HelperCaches.GetEntityTable(typeof(TTable), providerType);
            var exclude = new HashSet<string>(forceExclude ?? Array.Empty<string>(), StringComparer.InvariantCultureIgnoreCase);
            return new ChangedEntity<TTable>()
            {
                Entity = entityChangeSet.Entity,
                ModifiedFields = entityChangeSet.ModifiedFields.Where(name =>
            {
                var ep = entityTable.FindProperty(name);
                return ep != null && !ep.Ignore;
            }).Where(name => !exclude.Contains(name)).ToList()
            };
        }
    }
}