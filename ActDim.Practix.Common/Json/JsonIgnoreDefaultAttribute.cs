using System;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// Instructs <see cref="DefaultValueAwareResolver"/> to omit the property from JSON output
    /// when its value equals the default: the value from <see cref="System.ComponentModel.DefaultValueAttribute"/>
    /// if present, otherwise the CLR default for the property type (null / false / 0 / etc.).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class JsonIgnoreDefaultAttribute : Attribute
    {
    }
}
