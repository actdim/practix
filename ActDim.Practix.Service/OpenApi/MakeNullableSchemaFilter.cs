using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    /// <summary>
    /// MakeNullableParametersSchemaFilter
    /// </summary>
    public class MakeNullableSchemaFilter : ISchemaFilter
    {
        private static Type StringType = typeof(string);
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.ParameterInfo != default)
            {
                // var type = context.Type;
                var type = context.ParameterInfo.ParameterType;
                // TODO: support BindRequiredAttribute
                // TODO: support Nullable attribute from the System.Diagnostics.CodeAnalysis
                var nullable = StringType.Equals(type) || !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

                if (schema is OpenApiSchema openApiSchema)
                {
                    if (openApiSchema.Type.HasValue)
                    {
                        openApiSchema.Type |= JsonSchemaType.Null;
                    }
                    else
                    {
                        openApiSchema.Type = JsonSchemaType.Null;
                    }
                }

                if (schema.Extensions != default)
                {
                    schema.Extensions["x-nullable"] = new JsonNodeExtension(JsonValue.Create(true));
                }
            }
        }
    }
}