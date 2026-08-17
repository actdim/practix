using System;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Specifies a default value for a property or field during JSON serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonDefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDefaultValueAttribute"/> class with the specified default value.
        /// </summary>
        /// <param name="value">The default value.</param>
        public JsonDefaultValueAttribute(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the configured default value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the property should be pre-populated with <see cref="Value"/> prior to deserialization.
        /// </summary>
        public bool Populate { get; set; }
    }
}
