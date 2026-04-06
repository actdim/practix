using System;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// When applied to a collection property, instructs <see cref="EmptyCollectionIgnoreResolver"/>
    /// to omit the property from JSON output when the collection is null or empty.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class JsonIgnoreEmptyAttribute : Attribute
    {
    }
}
