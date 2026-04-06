using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Common.Json
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
    public class JsonNamingAttribute : Attribute
    {
        public JsonNamingPolicy Policy { get; }

        public JsonNamingAttribute(Type policyType = null)
        {
            Policy = policyType != null ? (JsonNamingPolicy)Activator.CreateInstance(policyType) : null;
        }
        public JsonNamingAttribute(JsonKnownNamingPolicy namingPolicy)
        {
            Policy = namingPolicy switch
            {
                JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
                JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
                JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
                JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
                _ => null,
            };
        }
    }
}
