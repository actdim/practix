using System;

namespace ActDim.Practix.Service.Json
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonDefaultValueAttribute : Attribute
    {
        public JsonDefaultValueAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public bool Populate { get; set; }
    }
}
