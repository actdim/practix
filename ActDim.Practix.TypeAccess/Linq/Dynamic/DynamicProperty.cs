using ActDim.Practix;
using Ardalis.GuardClauses;
using System;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
    public sealed class DynamicProperty
    {
        private readonly string _name;
        private readonly Type _type;
        private readonly int _hashCode;

        public DynamicProperty(string name, Type type)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Null(type, nameof(type));

            _name = name;
            _type = type;
            _hashCode = HashCodeHelper.CombineHashCode(new object[] { _name, _type });
        }

        public string Name
        {
            get { return _name; }
        }

        public Type Type
        {
            get { return _type; }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DynamicProperty);
        }

        public bool Equals(DynamicProperty other)
        {
            // Check for NULL
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            // Check for same reference
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(_name, other._name) && Equals(_type, other._type);
        }
    }
}
