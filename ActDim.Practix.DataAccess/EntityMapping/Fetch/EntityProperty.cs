using FastMember;
using System;
using System.Collections.Generic;
using System.Linq;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.DataAccess.Attributes;
using OrthoBits.Common;
using OrthoBits.Common.Extensions;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    internal sealed class EntityProperty
    {
        public readonly Type Type;

        public readonly string ColumnName;
        
        public readonly Type SerializerType;

        public readonly bool IsSerialized;

        public readonly bool Ignore;

        public readonly string Name;

        public readonly bool IsCollector;

        public readonly Type CustomCollectorType;

        public readonly int? Ordinal;

        public readonly Type PropertyActivatorType;

        public readonly int FetchOrder;

        public readonly bool IsSimple;

        public readonly bool IsValueTupleItem;

        public readonly bool Key;

        public readonly DbProviderType ProviderType;

        private EntityProperty(string name, Type type, bool isSerialized, Type serializerType, bool ignore,
            string columnName, int? ordinal, Type collectorType, Type propertyActivatorType, int fetchOrder,
            bool isSimple, bool key, bool isValueTupleItem)
        {
            Name = name;
            Type = type;
            IsSerialized = isSerialized;
            SerializerType = serializerType;
            Ignore = ignore;            
            ColumnName = columnName;
            Ordinal = ordinal;
            CustomCollectorType = collectorType;
            PropertyActivatorType = propertyActivatorType;
            FetchOrder = fetchOrder;
            IsSimple = isSimple;
            Key = key;
            IsValueTupleItem = isValueTupleItem;
        }

        internal EntityProperty(Member property, DbProviderType providerType, bool isValueTupleItem)
        {
            ProviderType = providerType;
            Type = property.Type;
            Name = property.Name;
            var dialect = HelperCaches.GetDialect(providerType);
            if (property.GetAttribute(typeof(IgnoreColumnAttribute), true)
                    is IgnoreColumnAttribute)
            {
                Ignore = true;
                return;
            }
            IsSimple = Type.IsSimple();
            string columnName = null;
            if (property.GetAttribute(typeof(ColumnAttribute), true) is ColumnAttribute ca)
            {
                IsSerialized = ca.IsSerialized;
                if (!string.IsNullOrWhiteSpace(ca.Name))
                {
                    columnName = ca.Name;
                }
                Key = ca.Key;
                SerializerType = ca.SerializerType;
                IsCollector = ca.IsCollector;
                CustomCollectorType = ca.CustomCollectorType;
                if (IsCollector && CustomCollectorType == null && !property.Type.GetInterfaces().Contains(typeof(IDictionary<string, object>)))
                {
                    throw new InvalidOperationException($"Can't write to non dictionary property without custom collector. Property: \"{property.Name}\", Type: \"{property.Type.FullName}\"");
                }
                Ordinal = ca.Ordinal == -1 ? (int?)null : ca.Ordinal;
                PropertyActivatorType = ca.PropertyActivatorType;
                FetchOrder = ca.FetchOrder;
            }
            if (string.IsNullOrWhiteSpace(columnName))
            {
                columnName = dialect.GetColumnName(property.Name);
            }
            ColumnName = dialect.PrepareIdent(columnName);
            IsValueTupleItem = isValueTupleItem;
        }

        public EntityProperty CreateVTableProperty(string namePrefix)
        {
            var dialect = HelperCaches.GetDialect(ProviderType);
            return new EntityProperty(Name, Type, IsSerialized, SerializerType, Ignore,
                dialect.PrepareIdent($"{namePrefix}.{ColumnName}"), Ordinal,
                CustomCollectorType, PropertyActivatorType, FetchOrder, IsSimple, Key,
                IsValueTupleItem);
        }
    }
}

