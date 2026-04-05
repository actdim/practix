namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    internal class PropertyToDbValueObj
    {
        public string ColumnName { set; get; }
        public string PropertyName { set; get; }
        public object Value { set; get; }

        public PropertyToDbValueObj(EntityProperty property, object value)
        {
            ColumnName = property.ColumnName;
            PropertyName = property.Name;
            Value = value;
        }
    }
}