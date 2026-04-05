
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public class OpenApiSchemaReference : IOpenApiSchema
    {
        private readonly string _schemaId;

        public OpenApiSchemaReference(string schemaId)
        {
            _schemaId = schemaId;
        }

        public string Title => default;
        public Uri Schema => default;
        public string Id => default;
        public string Comment => default;
        public IDictionary<string, bool> Vocabulary => default;
        public string DynamicRef => default;
        public string DynamicAnchor => default;
        public IDictionary<string, IOpenApiSchema> Definitions => default;
        public string ExclusiveMaximum => default;
        public string ExclusiveMinimum => default;
        public JsonSchemaType? Type => default;
        public string Const => default;
        public string Format => default;
        public string Maximum => default;
        public string Minimum => default;
        public int? MaxLength => default;
        public int? MinLength => default;
        public string Pattern => default;
        public decimal? MultipleOf => default;
        public JsonNode Default => default;
        public bool ReadOnly => false;
        public bool WriteOnly => false;
        public IList<IOpenApiSchema> AllOf => default;
        public IList<IOpenApiSchema> OneOf => default;
        public IList<IOpenApiSchema> AnyOf => default;
        public IOpenApiSchema Not => default;
        public ISet<string> Required => default;
        public IOpenApiSchema Items => default;
        public int? MaxItems => default;
        public int? MinItems => default;
        public bool? UniqueItems => default;
        public IDictionary<string, IOpenApiSchema> Properties => default;
        public IDictionary<string, IOpenApiSchema> PatternProperties => default;
        public int? MaxProperties => default;
        public int? MinProperties => default;
        public bool AdditionalPropertiesAllowed => true;
        public IOpenApiSchema AdditionalProperties => default;
        public OpenApiDiscriminator Discriminator => default;
        public JsonNode Example => default;
        public IList<JsonNode> Examples => default;
        public IList<JsonNode> Enum => default;
        public bool UnevaluatedProperties => false;
        public OpenApiExternalDocs ExternalDocs => default;
        public bool Deprecated => false;
        public OpenApiXml Xml => default;
        public IDictionary<string, IOpenApiExtension> Extensions => default;
        public IDictionary<string, JsonNode> UnrecognizedKeywords => default;
        public IDictionary<string, HashSet<string>> DependentRequired => default;

        public string Description { get => null; set => throw new NotImplementedException(); }

        public void SerializeAsV31(IOpenApiWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("$ref", $"#/components/schemas/{_schemaId}");
            writer.WriteEndObject();
        }

        public void SerializeAsV3(IOpenApiWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("$ref", $"#/components/schemas/{_schemaId}");
            writer.WriteEndObject();
        }

        public void SerializeAsV2(IOpenApiWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("$ref", $"#/definitions/{_schemaId}");
            writer.WriteEndObject();
        }

        public IOpenApiSchema CreateShallowCopy() => new OpenApiSchemaReference(_schemaId);
    }
}