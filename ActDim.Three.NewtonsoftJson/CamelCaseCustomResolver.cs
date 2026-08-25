using System;
using Newtonsoft.Json.Serialization;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// Custom camelCase contract resolver for Newtonsoft.Json that preserves dictionary key names.
    /// </summary>
    public class CamelCaseCustomResolver : CamelCasePropertyNamesContractResolver
    {
        /// <inheritdoc />
        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
        {
            JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);
            contract.DictionaryKeyResolver = propertyName => propertyName;
            return contract;
        }
    }
}
