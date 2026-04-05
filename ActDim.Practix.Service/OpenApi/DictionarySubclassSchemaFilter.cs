using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public class DictionarySubclassSchemaFilter : ISchemaFilter
    {
        private static readonly Type DictionaryType = typeof(Dictionary<string, object>);
        private static readonly Type GenericDictionaryType = typeof(Dictionary<,>);
        private static readonly Type GenericDictionaryInterfaceType = typeof(IDictionary<,>);
        private readonly AppSettings _appSettings;
        public DictionarySubclassSchemaFilter(IOptions<AppSettings> options)
        {
            _appSettings = options.Value;
        }
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (DictionaryType.IsAssignableFrom(context.Type)
                && context.Type != DictionaryType)
            {
                if (schema is OpenApiSchema openApiSchema)
                {
                    openApiSchema.Type = JsonSchemaType.Object;
                    openApiSchema.AdditionalPropertiesAllowed = true;
                    openApiSchema.AdditionalProperties = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object
                    };
                }
            }
            if (context.Type.IsGenericType && GenericDictionaryType.Equals(context.Type.GetGenericTypeDefinition()))
            {
                var keyType = context.Type.GenericTypeArguments[0];
                if (keyType.IsEnum)
                {
                    var enumSchemaId = _appSettings == default ? keyType.GetOpenApiSchemaId() : keyType.GetOpenApiSchemaId(_appSettings.SchemaPrefix, _appSettings.ClassPrefix);

                    if (context.SchemaRepository.Schemas.TryGetValue(enumSchemaId, out _))
                    {
                        var properties = schema.Properties.ToList();

                        schema.Properties.Clear();
                        var propertySchema = properties.First().Value;

                        var dataSchema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object | JsonSchemaType.Null,
                            Extensions = new Dictionary<string, IOpenApiExtension>
                            {
                                ["x-dictionaryKey"] = new JsonNodeExtension(new JsonObject
                                {
                                    ["$ref"] = JsonValue.Create($"#/components/schemas/{enumSchemaId}")
                                })
                            },
                            AdditionalProperties = propertySchema
                        };

                        if (schema is OpenApiSchema openApiSchema)
                        {
                            if (schema.AllOf == default)
                            {
                                openApiSchema.AllOf = [];
                            }
                            schema.AllOf.Clear();
                            schema.AllOf.Add(dataSchema);
                        }

                        // workaround:
                        // var enumValues = Enum.GetValues(keyType);
                        // for (var i = 0; i < properties.Count; i++)
                        // {
                        //     schema.Properties.Add(enumValues.GetValue(i).ToString(), properties[i].Value);
                        // }
                    }
                }
            }
        }
    }
}