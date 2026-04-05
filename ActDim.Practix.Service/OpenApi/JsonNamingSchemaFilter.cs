using ActDim.Practix.Service.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.OpenApi
{
    public class JsonNamingSchemaFilter : ISchemaFilter
    {
        private readonly JsonNamingPolicy _globalNamingPolicy;

        // public JsonNamingSchemaFilter(IOptions<JsonOptions> jsonOptions)
        // {
        //     _globalNamingPolicy = jsonOptions.Value.JsonSerializerOptions.PropertyNamingPolicy;
        // }

        public JsonNamingSchemaFilter()
        {
            _globalNamingPolicy = null;
        }

        public JsonNamingSchemaFilter(JsonNamingPolicy globalNamingPolicy)
        {
            _globalNamingPolicy = globalNamingPolicy;
        }

        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            var classAttr = context.Type.GetCustomAttribute<JsonNamingAttribute>();

            // With UseAllOfForInheritance(), derived class properties are placed inside
            // an inline allOf element rather than directly in schema.Properties.
            // Find the schema that actually contains the properties.
            var targetSchema = schema.Properties?.Any() == true
                ? schema
                : schema.AllOf?.FirstOrDefault(s => s.Properties?.Any() == true);

            if (targetSchema?.Properties == null || !targetSchema.Properties.Any())
                return;

            var renames = new Dictionary<string, string>();

            foreach (var clrProp in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (clrProp.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
                    continue;

                var ignoreAttr = clrProp.GetCustomAttribute<JsonIgnoreAttribute>();
                if (ignoreAttr?.Condition == JsonIgnoreCondition.Always)
                    continue;

                // Priority: property-level [JsonNaming] > class-level [JsonNaming] > global
                // NOTE: cannot use ?? because null policy means "identity" (PascalCase),
                // which is different from "no attribute" (fall through to next level)
                var propAttr = clrProp.GetCustomAttribute<JsonNamingAttribute>();
                JsonNamingPolicy effectivePolicy;
                if (propAttr != null)
                    effectivePolicy = propAttr.Policy;
                else if (classAttr != null)
                    effectivePolicy = classAttr.Policy;
                else
                    effectivePolicy = _globalNamingPolicy;

                var clrName = clrProp.Name;
                var desiredName = effectivePolicy?.ConvertName(clrName) ?? clrName;

                var currentName = targetSchema.Properties.Keys
                    .FirstOrDefault(k => string.Equals(k, clrName, StringComparison.OrdinalIgnoreCase));

                if (currentName != null && currentName != desiredName)
                    renames[currentName] = desiredName;
            }

            foreach (var (oldName, newName) in renames)
            {
                var propSchema = targetSchema.Properties[oldName];
                targetSchema.Properties.Remove(oldName);
                targetSchema.Properties[newName] = propSchema;

                if (targetSchema.Required?.Remove(oldName) == true)
                    targetSchema.Required.Add(newName);
            }
        }
    }
}
