using FastMember;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.Json;
using System.Buffers;
using OrthoBits.Common;
using OrthoBits.Common.Extensions;
using Newtonsoft.Json;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    public class EntityFetcher
    {
        private readonly IDictionary<string, int> _columnOrdinalMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, Type> _columnTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private readonly EntityTable _entityTable;
        private readonly ISqlDialect _conventionProjector;
        private readonly FetcherEntityFactory _entityFactory;

        internal EntityFetcher(DbDataReader dataReader, EntityTable entityTable, FetcherEntityFactory entityFactory)
        {
            _entityTable = entityTable;
            _entityFactory = entityFactory;
            _conventionProjector = HelperCaches.GetDialect(entityTable.ProviderType);
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var name = dataReader.GetName(i);
                // var ordinal = dataReader.GetOrdinal(name);
                _columnOrdinalMap[name] = i;
                _columnTypeMap[name] = dataReader.GetFieldType(i);
            }
        }

        private object FetchComplexProperty(object context, TypeAccessor contextTypeAccessor,
            EntityProperty property, DbDataReader dataReader, IJsonSerializer stdSerializer,
            bool isMemberOfTuple)
        {
            var propertyVTable = HelperCaches.GetEntityTable(property.Type, _entityTable.ProviderType)
                .CreateVTable(property.ColumnName);
            var propertyFetcher = new EntityFetcher(dataReader, propertyVTable, _entityFactory);
            object instance = null;
            if (property.PropertyActivatorType != null)
            {
                var activator = HelperCaches.GetPropertyActivator(property.PropertyActivatorType);
                instance = activator.CreateInstance(context, property.Name);
                if (instance == null)
                {
                    throw new InvalidOperationException($"Activator of Property \"{property.Name}\" returned null");
                }
            }
            else
            {
                if (!isMemberOfTuple)
                {
                    instance = contextTypeAccessor[context, property.Name];
                    if (instance == null)
                    {
                        instance = TypeAccessor.Create(property.Type).CreateNew();
                    }
                }
            }
            return propertyFetcher.FetchInternal(dataReader, stdSerializer, instance);
        }

        private MemoryStream GetColumnStream(DbDataReader dataReader, int ordinal)
        {
            var byteCount = (int)dataReader.GetBytes(ordinal, 0, null, 0, 0); // size

            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);

            try
            {
                var count = dataReader.GetBytes(ordinal, 0, buffer, 0, byteCount);

                if (count < byteCount)
                {
                    throw new InvalidOperationException("Can't read BLOB");
                }

                // result               
                var stream = MemoryManager.Default.GetStream(nameof(EntityFetcher), buffer, 0, byteCount);
                // stream.Position = 0;
                return stream;

            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private bool EnsureKeyColumns(EntityTable table, DbDataReader dataReader)
        {
            var keyProperties = table.Properties.Where(x => x.Key).ToList();
            if (keyProperties.Count == 0)
            {
                // no keys are set
                // that is ok for custom queries (without keys)
                return true;
            }

            foreach (var keyProperty in keyProperties)
            {
                if (!keyProperty.IsSimple)
                {
                    throw new InvalidOperationException($"{keyProperty.Name} of {table.EntityType} can not be a Key. " +
                                        $"Only simple properties are supported");
                }

                if (!_columnOrdinalMap.TryGetValue(keyProperty.ColumnName, out var ordinal))
                {
                    throw new InvalidOperationException($"Key column {keyProperty.ColumnName} does not exist in result set. " +
                                        $"Unable to set {keyProperty.Name} of {table.EntityType}");
                }

                if (dataReader.IsDBNull(ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly Type TypeOfByteArray = typeof(byte[]);

        private object FetchInternal(DbDataReader dataReader, IJsonSerializer defaultSerializer,
            object instance = null)
        {
            var isTuple = this._entityTable.IsTuple;
            // using special temporary storage for tuplesto
            var tupleStore = isTuple ? new Dictionary<string, object>() : null;

            var columnNames = new HashSet<string>(_columnOrdinalMap.Keys);
            var accessor = TypeAccessor.Create(_entityTable.EntityType, true);
            var table = _entityTable;
            var hasKeys = !isTuple && EnsureKeyColumns(table, dataReader);

            if (!isTuple && hasKeys && instance == null)
            {
                instance = this._entityFactory
                    .CreateEntity(_entityTable.EntityType);
            }
            int ordinal;
            object value = null;
            foreach (var property in table.Properties
                .Where(x => !x.IsCollector && !x.Ignore)
                .OrderBy(x => !x.IsSimple)
                .ThenBy(x => x.FetchOrder))
            {
                if (!property.IsSerialized && !property.IsSimple)
                {
                    if (isTuple)
                    {
                        tupleStore[property.Name] = FetchComplexProperty(instance,
                            accessor, property, dataReader, defaultSerializer, true);
                    }
                    else
                    {
                        accessor[instance, property.Name] = FetchComplexProperty(instance,
                            accessor, property, dataReader, defaultSerializer, false);
                    }

                    continue;
                }

                if (!_columnOrdinalMap.TryGetValue(property.ColumnName, out ordinal))
                {
                    // should we raise an error?
                    continue;
                }

                if (property.Ordinal != null)
                {
                    if (dataReader.FieldCount <= property.Ordinal)
                    {
                        throw new InvalidOperationException($"The Property {property.Name} has {property.Ordinal} ordinal but row contains {dataReader.FieldCount} columns");
                    }
                    ordinal = property.Ordinal.Value;
                }

                columnNames.Remove(property.ColumnName);

                if (dataReader.IsDBNull(ordinal))
                {
                    value = null;
                    if (!hasKeys)
                    {
                        // it`s ok for JOINs
                        // just ignore
                        // otherwise code below should fail
                        continue;
                    }
                }
                else
                {
                    if (!hasKeys && !isTuple)
                    {
                        throw new InvalidOperationException($"One or more Keys of {table.EntityType} are not set, " +
                                                            $"but {property.Name} (column {property.ColumnName}) is not NULL in result set.");
                    }

                    Action finalize = null;
                    try
                    {
                        object getBytes(bool asStream)
                        {
                            var stream = GetColumnStream(dataReader, ordinal);
                            finalize = () =>
                            {
                                if (!ReferenceEquals(stream, value))
                                {
                                    stream.Dispose();
                                }
                            };
                            if (asStream)
                            {
                                value = stream;
                            }
                            else
                            {
                                // var arraySegment = new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length);
                                if (!stream.TryGetBuffer(out ArraySegment<byte> arraySegment))
                                {
                                    throw new InvalidOperationException("Stream is not exposable");
                                }
                                value = arraySegment;
                            }
                            return value;
                        }

                        if (property.IsSerialized)
                        {
                            if (property.SerializerType == null)
                            {
                                string strValue = null;
                                if (_columnTypeMap[property.ColumnName] == TypeOfByteArray)
                                {
                                    getBytes(true);
                                    var stream = (Stream)value;
                                    strValue = stream.ToString(System.Text.Encoding.UTF8);
                                }
                                else
                                {
                                    value = dataReader.GetValue(ordinal);
                                    strValue = value as string;
                                }

                                var serializerSettings = new JsonSerializerSettings(defaultSerializer.SerializerSettings);
                                // defaultSerializer.PopulateDefaultSerializerSettings(serializerSettings); // alternative way
                                serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                                value = string.IsNullOrWhiteSpace(strValue) ? null :
                                    defaultSerializer.DeserializeObject(strValue, property.Type, serializerSettings);
                            }
                            else
                            {
                                var serializer = HelperCaches.GetSerializer(property.SerializerType);
                                if (_columnTypeMap[property.ColumnName] == TypeOfByteArray)
                                {
                                    getBytes(true);
                                }
                                else
                                {
                                    value = dataReader.GetValue(ordinal);
                                }

                                if (value.GetType() != property.Type)
                                {
                                    var newValue = serializer.Deserialize(value);
                                    if (ReferenceEquals(newValue, value))
                                    {
                                        if (newValue is Stream stream)
                                        {
                                            throw new InvalidOperationException("Using original stream as property value is not allowed");
                                        }
                                    }
                                    value = newValue;
                                }
                            }
                        }
                        else
                        {
                            if (_columnTypeMap[property.ColumnName] == TypeOfByteArray)
                            {
                                getBytes(false);
                                value = ((ArraySegment<byte>)value).CloneToArray();
                            }
                            else
                            {
                                value = dataReader.GetValue(ordinal);
                            }
                            value = _conventionProjector.ProjectDbValue(property.Type, value);
                        }
                    }
                    finally
                    {
                        finalize?.Invoke();
                    }
                }

                try
                {
                    if (isTuple)
                    {
                        tupleStore[property.Name] = value;
                    }
                    else
                    {
                        accessor[instance, property.Name] = value;
                    }
                }
                catch (Exception e)
                {
                    if (value == null)
                    {
                        throw new InvalidOperationException($"Trying to set Null to {property.Name} property of {_entityTable.EntityType}. " +
                                            $"Expected type is {property.Type}", e);
                    }

                    throw new InvalidOperationException($"Unable to set {property.Name} property of {_entityTable.EntityType}. " +
                                        $"Source type is {value.GetType()} but destination type is {property.Type}", e);
                }
            }

            var collectorProperty = table.Properties.SingleOrDefault(p => p.IsCollector);
            if (collectorProperty != null)
            {
                if (!hasKeys)
                {
                    throw new InvalidOperationException($"One or more Keys of {table.EntityType} are not defined, " +
                                                        $"unable to use Custom Collector.");
                }

                var collectorType = collectorProperty.CustomCollectorType;
                foreach (var columnName in columnNames)
                {
                    ordinal = _columnOrdinalMap[columnName];
                    value = dataReader.GetValue(ordinal);
                    if (collectorType != null)
                    {
                        var writer = HelperCaches.GetPropertyWriter(collectorType);
                        writer.Write(instance, columnName, value);
                    }
                    else
                    {
                        var destination = (IDictionary<string, object>)accessor[instance, collectorProperty.Name];
                        destination[columnName] = value;
                    }
                }
            }

            if (isTuple)
            {
                return CreateValueTupleObject(tupleStore);
            }

            if (!hasKeys && instance != null)
            {
                instance = null;
            }

            return instance;
        }

        private object CreateValueTupleObject(Dictionary<string, object> tupleStore)
        {
            var count = _entityTable.Properties.Count;
            // fill out missing properties
            for (var i = 0; i < count; ++i)
            {
                var name = $"Item{i + 1}";
                if (!tupleStore.ContainsKey(name))
                {
                    tupleStore[name] = _entityFactory.CreateEntity(
                        _entityTable.FindProperty(name).Type);
                }
            }
            switch (count)
            {
                case 1:
                    {
                        var type = typeof(ValueTuple<>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type);
                        return Activator.CreateInstance(type, tupleStore["Item1"]);
                    }
                case 2:
                    {
                        var type = typeof(ValueTuple<,>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type,
                                _entityTable.FindProperty("Item2").Type);
                        return Activator.CreateInstance(type,
                            tupleStore["Item1"],
                            tupleStore["Item2"]);
                    }
                case 3:
                    {
                        var type = typeof(ValueTuple<,,>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type,
                                _entityTable.FindProperty("Item2").Type,
                                _entityTable.FindProperty("Item3").Type);
                        return Activator.CreateInstance(type,
                            tupleStore["Item1"],
                            tupleStore["Item2"],
                            tupleStore["Item3"]);
                    }
                case 4:
                    {
                        var type = typeof(ValueTuple<,,,>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type,
                                _entityTable.FindProperty("Item2").Type,
                                _entityTable.FindProperty("Item3").Type,
                                _entityTable.FindProperty("Item4").Type);
                        return Activator.CreateInstance(type,
                            tupleStore["Item1"],
                            tupleStore["Item2"],
                            tupleStore["Item3"],
                            tupleStore["Item4"]);
                    }
                case 5:
                    {
                        var type = typeof(ValueTuple<,,,,>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type,
                                _entityTable.FindProperty("Item2").Type,
                                _entityTable.FindProperty("Item3").Type,
                                _entityTable.FindProperty("Item4").Type,
                                _entityTable.FindProperty("Item5").Type);
                        return Activator.CreateInstance(type,
                            tupleStore["Item1"],
                            tupleStore["Item2"],
                            tupleStore["Item3"],
                            tupleStore["Item4"],
                            tupleStore["Item5"]);
                    }
                case 6:
                    {
                        var type = typeof(ValueTuple<,,,,,>)
                            .MakeGenericType(
                                _entityTable.FindProperty("Item1").Type,
                                _entityTable.FindProperty("Item2").Type,
                                _entityTable.FindProperty("Item3").Type,
                                _entityTable.FindProperty("Item4").Type,
                                _entityTable.FindProperty("Item5").Type,
                                _entityTable.FindProperty("Item6").Type);
                        return Activator.CreateInstance(type,
                            tupleStore["Item1"],
                            tupleStore["Item2"],
                            tupleStore["Item3"],
                            tupleStore["Item4"],
                            tupleStore["Item5"],
                            tupleStore["Item6"]);
                    }
            }

            throw new InvalidOperationException("can not create tuple");
        }

        public object Fetch(DbDataReader dataReader, IJsonSerializer defaultSerializer)
        {
            return FetchInternal(dataReader, defaultSerializer);
        }
    }
}

