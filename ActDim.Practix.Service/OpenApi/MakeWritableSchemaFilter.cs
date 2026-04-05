using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ActDim.Practix.Service.OpenApi
{
    /// <summary>
    /// MakeWritablePropertiesSchemaFilter
    /// </summary>
    public class MakeWritableSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties != null)
            {
                // .Where(p => p.ReadOnly)
                foreach (var property in schema.Properties.Values)
                {
                    if (property is OpenApiSchema openApiSchema)
                    {
                        openApiSchema.ReadOnly = false;
                        // openApiSchema.WriteOnly?
                    }
                }
            }
        }
    }
}