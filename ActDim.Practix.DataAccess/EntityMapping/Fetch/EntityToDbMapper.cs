using FastMember;
using System;
using System.Collections.Generic;
using System.Linq;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Common.Extensions;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    internal class EntityToDbMapper
    {
        private readonly EntityTable _table;
        private readonly TypeAccessor _accessor;
        private readonly ISqlDialect _conventionProjector;
        private readonly IJsonSerializer _jsonSerializer;

        internal EntityToDbMapper(EntityTable table, IJsonSerializer jsonSerializer)
        {
            _table = table;
            _jsonSerializer = jsonSerializer;
            _accessor = TypeAccessor.Create(_table.EntityType);
            _conventionProjector = HelperCaches.GetDialect(_table.ProviderType);
        }

        private IEnumerable<PropertyToDbValueObj> GetNestedObjectMappedValues(
            object nestedEntity,
            EntityProperty property,
            IEnumerable<string> include)
        {
            var nestedTable = HelperCaches.GetEntityTable(property.Type, _table.ProviderType);
            if (!include.Any())
            {
                // should collect all
                include = nestedTable.Properties.Select(x => x.Name);
            }
            if (nestedTable.TableName != null && nestedTable.TableName != _table.TableName)
            {
                // nested object is not related to current table
                return Array.Empty<PropertyToDbValueObj>();
            }
            var nestedMapper = new EntityToDbMapper(nestedTable, _jsonSerializer);
            var values = nestedMapper.MapToDbValues(nestedEntity, include);
            return values;
        }

        public List<PropertyToDbValueObj> MapToDbValues(
            object entity,
            IEnumerable<string> include)
        {
            var result = new List<PropertyToDbValueObj>();
            foreach (var modifiedFieldFullPath in include)
            {
                var modifiedField = modifiedFieldFullPath.Contains('.') ?
                    modifiedFieldFullPath.Split('.').First() :
                    modifiedFieldFullPath;

                var property = _table.FindProperty(modifiedField);
                if (property == null)
                {
                    throw new InvalidOperationException($"Entity {_table.EntityType} has no property `{modifiedField}`");
                }
                if (property.Ignore)
                {
                    continue;
                }
                if (entity == null)
                {
                    if (!property.IsSimple && !property.IsCollector && !property.IsSerialized)
                    {
                        var nestedInclude = include.Where(x => x.StartsWith(property.Name + '.'))
                            .Select(x =>
                            {
                                var idx = x.IndexOf('.');
                                return x.Substring(idx + 1);
                            });
                        result.AddRange(GetNestedObjectMappedValues(null, property, nestedInclude));
                        continue;
                    }
                    result.Add(new PropertyToDbValueObj(property, property.Type.GetDefaultValue()));
                    continue;
                }
                var value = _accessor[entity, property.Name];
                if (value == null)
                {
                    if (!property.IsSimple && !property.IsCollector && !property.IsSerialized)
                    {
                        var nestedInclude = include.Where(x => x.StartsWith(property.Name + '.'))
                            .Select(x =>
                            {
                                var idx = x.IndexOf('.');
                                return x.Substring(idx + 1);
                            });
                        result.AddRange(GetNestedObjectMappedValues(null, property, nestedInclude));
                        continue;
                    }
                    result.Add(new PropertyToDbValueObj(property, property.Type.GetDefaultValue()));
                    continue;
                }
                if (property.IsSerialized)
                {
                    object serializedValue;
                    if (property.SerializerType != null)
                    {
                        var typeSerializer = HelperCaches.GetSerializer(property.SerializerType);
                        serializedValue = typeSerializer.Serialize(value);
                    }
                    else
                    {
                        serializedValue = _jsonSerializer.SerializeObject(value);
                    }
                    result.Add(new PropertyToDbValueObj(property, serializedValue));
                    continue;
                }
                if (!property.IsSimple)
                {
                    var nestedInclude = include.Where(x => x.StartsWith(property.Name + '.'))
                        .Select(x =>
                        {
                            var idx = x.IndexOf('.');
                            return x.Substring(idx + 1);
                        });
                    result.AddRange(GetNestedObjectMappedValues(value, property, nestedInclude));
                    continue;
                }
                var projectedValue = _conventionProjector.ProjectEntityValue(value);
                result.Add(new PropertyToDbValueObj(property, projectedValue));
            }
            return result;
        }

    }
}
