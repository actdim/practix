using Conditions;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.DataAccess.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace OrthoBits.DataAccess
{
    internal class IdGenerator : IIdGenerator
    {
        private readonly IDictionary<DbProviderType, ISequenceIdGenerator> _generators =
            new Dictionary<DbProviderType, ISequenceIdGenerator>();

        private readonly IDictionary<Type, (DbProviderType, string)> _entitySequenceParamsCache =
            new ConcurrentDictionary<Type, (DbProviderType, string)>();

        public IdGenerator(IEnumerable<ISequenceIdGenerator> generators)
        {
            foreach (var sequenceIdGenerator in generators)
            {
                _generators[sequenceIdGenerator.ProviderType] = sequenceIdGenerator;
            }
        }

        public long GetNewId(DbProviderType providerType, string sequenceName)
        {
            Condition.Ensures(_generators.TryGetValue(providerType, out var generator)).IsTrue($"{providerType} database generator is not registered");
            return generator.GetNewId(sequenceName);
        }

        public long GetNewId<T>()
        {
            var type = typeof(T);
            if (!_entitySequenceParamsCache.TryGetValue(type, out var sequenceParams))
            {
                var tableAttribute = type.GetCustomAttribute(typeof(TableAttribute), true) as TableAttribute;
                Condition.Ensures(tableAttribute).IsNotNull($"Entity {type.Name} has no {nameof(TableAttribute)} attribute");
                string name = "";
                switch (tableAttribute.ProviderType)
                {
                    // TODO: check if necessary
                    case DbProviderType.PostgreSQL:
                        name = $"{tableAttribute.Name.ToLowerInvariant()}_id_seq";
                        break;
                    default:
                        name = $"G_{tableAttribute.Name.ToUpperInvariant()}";
                        break;
                }
                _entitySequenceParamsCache[type] = (tableAttribute.ProviderType, name);
                sequenceParams = (tableAttribute.ProviderType, name);
            }
            return GetNewId(sequenceParams.Item1, sequenceParams.Item2);
        }

        public long GetNewId(string sequenceName)
        {
            throw new NotImplementedException();
        }
    }
}
