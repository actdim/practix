using OrthoBits.Abstractions.DataAccess;

namespace OrthoBits.DataAccess.Attributes
{
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }

        public bool IsSerialized { get; }

        public Type SerializerType { get; }

        public bool IsCollector { get; }

        public Type CustomCollectorType { get; }

        public int Ordinal { get; } = -1;

        public int FetchOrder { get; }

        public Type PropertyActivatorType { get; }

        public bool Key { set; get; }

        public object DefaultValue { get; }

        public ColumnAttribute()
        {
        }

        public ColumnAttribute(string name, Type propertyActivatorType, int fetchOrder = 0)
        {
            Name = name;
            FetchOrder = fetchOrder;
            if (propertyActivatorType != null && propertyActivatorType.GetInterfaces().All(x => x != typeof(IPropertyActivator)))
            {
                throw new InvalidOperationException($"{nameof(propertyActivatorType)} must implement {nameof(IPropertyActivator)}");
            }
            PropertyActivatorType = propertyActivatorType;
        }

        public ColumnAttribute(Type propertyActivatorType, int fetchOrder = 0) :
            this(null, propertyActivatorType, fetchOrder)
        {
        }

        public ColumnAttribute(int ordinal, Type propertyActivatorType, int fetchOrder = 0) :
            this(null, propertyActivatorType, fetchOrder)
        {
            Ordinal = ordinal;
        }

        public ColumnAttribute(int ordinal, int fetchOrder = 0)
        {
            FetchOrder = fetchOrder;
            Ordinal = ordinal;
        }

        public ColumnAttribute(int ordinal, bool isSerialized, int fetchOrder = 0)
        {
            Ordinal = ordinal;
            IsSerialized = isSerialized;
            FetchOrder = fetchOrder;
        }

        public ColumnAttribute(int ordinal, bool isSerialized, Type serializerType, int fetchOrder = 0)
        {
            IsSerialized = isSerialized;
            SerializerType = serializerType;
            Ordinal = ordinal;
            FetchOrder = fetchOrder;
        }

        public ColumnAttribute(bool isCollector, Type customCollectorType, string namePattern, int fetchOrder = 0) :
            this(namePattern, false, null, fetchOrder)
        {
            IsCollector = isCollector;
            CustomCollectorType = customCollectorType;
        }

        public ColumnAttribute(bool isSerialized, int fetchOrder = 0) :
            this(null, isSerialized, null, fetchOrder)
        {
        }

        public ColumnAttribute(bool isSerialized, Type serializerType, int fetchOrder = 0) :
            this(null, isSerialized, serializerType, fetchOrder)
        {
        }

        public ColumnAttribute(string name, int fetchOrder = 0) :
            this(name, false, null, fetchOrder)
        {
        }

        public ColumnAttribute(string name, object defaultValue, int fetchOrder = 0) :
            this(name, false, null, fetchOrder)
        {
            DefaultValue = defaultValue;
        }

        public ColumnAttribute(string name, bool isSerialized, int fetchOrder = 0) :
            this(name, isSerialized, null, fetchOrder)
        {
        }

        public ColumnAttribute(string name, bool isSerialized, Type serializerType, int fetchOrder = 0)
        {
            Name = name;
            FetchOrder = fetchOrder;
            IsSerialized = isSerialized;
            if (serializerType != null && serializerType.GetInterfaces().All(x => x != typeof(IColumnSerializer)))
            {
                throw new InvalidOperationException($"{nameof(serializerType)} must implement {nameof(IColumnSerializer)}");
            }
            SerializerType = serializerType;
        }
    }
}
