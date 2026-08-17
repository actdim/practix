using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Specifies a property naming policy (<see cref="JsonNamingPolicy"/>) to be applied to a class, struct, or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
    public class JsonNamingAttribute : Attribute
    {
        /// <summary>
        /// Gets the resolved naming policy instance.
        /// </summary>
        public JsonNamingPolicy Policy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNamingAttribute"/> class using a custom naming policy type.
        /// </summary>
        /// <param name="policyType">A type inheriting from <see cref="JsonNamingPolicy"/>.</param>
        public JsonNamingAttribute(Type policyType = null)
        {
            Policy = policyType != null ? (JsonNamingPolicy)Activator.CreateInstance(policyType) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNamingAttribute"/> class using a known naming policy enum.
        /// </summary>
        /// <param name="namingPolicy">The known naming policy enum value.</param>
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
