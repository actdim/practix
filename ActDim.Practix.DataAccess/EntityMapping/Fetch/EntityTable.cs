using FastMember;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrthoBits.DataAccess.Attributes;
using OrthoBits.Abstractions.DataAccess;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    internal sealed class EntityTable
    {
        private readonly Dictionary<string, EntityProperty> _propertyMap;
        public readonly DbProviderType ProviderType; // SQL Convention
        public readonly List<EntityProperty> Properties;
        public readonly Type EntityType;
        public readonly string TableName;
        public bool IsTuple;
        public int TupleValuesCount;

        public EntityTable()
        {
            _propertyMap = new Dictionary<string, EntityProperty>(StringComparer.InvariantCultureIgnoreCase);
        }

        private void RebuildPropertyMap()
        {
            _propertyMap.Clear();
            foreach (var property in Properties)
            {
                _propertyMap[property.Name] = property;
            }
        }

        private EntityTable(DbProviderType convention, Type entityType) : this()
        {
            ProviderType = convention;
            EntityType = entityType;
            Properties = new List<EntityProperty>();
        }

        private EntityTable(DbProviderType convention, Type entityType, int tupleValuesCount)
            : this(convention, entityType)
        {
            IsTuple = true;
            TupleValuesCount = tupleValuesCount;
        }

        internal EntityTable(TypeAccessor accessor, Type entityType, DbProviderType providerType = default) : this()
        {
            if (entityType.GetCustomAttribute(typeof(TableAttribute), true) is TableAttribute ta)
            {
                if (providerType != default && providerType != ta.ProviderType)
                {
                    throw new InvalidOperationException($@"DB provider type mismatch: ""{ta.ProviderType}"" is used in ""{entityType.FullName}"" table metadata, but ""{providerType}"" is expected in query"); // query expects ...
                }
                this.ProviderType = ta.ProviderType;
                this.TableName = ta.Name;
            }
            else
            {
                if (providerType == default)
                {
                    throw new InvalidOperationException(
                        $"Unable to create Entity table for {entityType.FullName}. {nameof(TableAttribute)} is required");
                }
                this.ProviderType = providerType;
            }

            if (IsValueTuple(entityType, out var itemsCount))
            {
                IsTuple = true;
                TupleValuesCount = itemsCount;
                var member = accessor.GetMembers();
                Properties = member.Select(m =>
                    new EntityProperty(m, ProviderType, true)).ToList();
            }
            else
            {
                var member = accessor.GetMembers();
                Properties = member.Where(x => x.CanWrite).Select(m =>
                   new EntityProperty(m, ProviderType, false)).ToList();
            }
            EntityType = entityType;
            RebuildPropertyMap();
        }

        private static bool IsValueTuple(Type type, out int itemsCount)
        {
            itemsCount = 0;
            if (!type.IsGenericType)
            {
                return false;
            }

            var gType = type.GetGenericTypeDefinition();
            if (gType == typeof(ValueTuple<>))
            {
                itemsCount = 1;
                return true;
            }
            if (gType == typeof(ValueTuple<,>))
            {
                itemsCount = 2;
                return true;
            }
            if (gType == typeof(ValueTuple<,,>))
            {
                itemsCount = 3;
                return true;
            }
            if (gType == typeof(ValueTuple<,,,>))
            {
                itemsCount = 4;
                return true;
            }
            if (gType == typeof(ValueTuple<,,,,>))
            {
                itemsCount = 5;
                return true;
            }
            if (gType == typeof(ValueTuple<,,,,,>))
            {
                itemsCount = 6;
                return true;
            }
            if (gType == typeof(ValueTuple<,,,,,,>))
            {
                itemsCount = 7;
                return true;
            }
            if (gType == typeof(ValueTuple<,,,,,,,>))
            {
                itemsCount = 8;
                return true;
            }
            return false;
        }

        public EntityProperty FindProperty(string name)
        {
            _propertyMap.TryGetValue(name, out var property);

            // Is it a boolean property adapter for Y/N? Use pattern <Field>Code
            if (property.Type == typeof(bool) && property.Name.StartsWith("Is") && property.Name == property.ColumnName)
            {
                name = name.Remove(0, 2) + "Code";
                if (_propertyMap.TryGetValue(name, out var codeProperty))
                {
                    return codeProperty;
                }
            }

            // Is it a string property adapter for BLOB?  Use pattern <Field>Raw
            else if (property.Type == typeof(string) && property.Name == property.ColumnName)
            {
                name = name + "Raw";
                if (_propertyMap.TryGetValue(name, out var rawDataProperty))
                {
                    return rawDataProperty;
                }
            }

            return property;
        }

        public EntityTable CreateVTable(string namePrefix)
        {
            var table = IsTuple ? new EntityTable(ProviderType, EntityType, TupleValuesCount) :
                new EntityTable(ProviderType, EntityType);
            table.Properties.AddRange(Properties.Select(x => x.CreateVTableProperty(namePrefix)));
            table.RebuildPropertyMap();
            return table;
        }
    }
}