using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public class ApiEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Enum == null || !schema.Enum.Any() || !context.Type.IsEnum)
            {
                return;
            }

            var names = Enum.GetNames(context.Type);
            var enumValues = Enum.GetValues(context.Type);

            schema.Enum.Clear();
            if (schema.Extensions == default)
            {
                if (schema is OpenApiSchema openApiSchema)
                {
                    openApiSchema.Extensions = new Dictionary<string, IOpenApiExtension>();
                }
            }
            schema.Extensions.Clear();

            var jsonValuesArray = new JsonArray();
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = enumValues.GetValue(i);

                var field = context.Type.GetField(name);
                var descriptionAttr = (DescriptionAttribute)field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                   .FirstOrDefault();
                var enumMemberAttr = (EnumMemberAttribute)field.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                    .FirstOrDefault();
                if (enumMemberAttr != default && enumMemberAttr.Value != default)
                {
                    value = enumMemberAttr.Value;
                }

                var jsonNameValue = JsonValue.Create(name);
                var jsonValue = value is string stringValue ? JsonValue.Create(stringValue) : JsonValue.Create(Convert.ToInt64(value));

                schema.Enum.Add(jsonValue);
                var valueObject = new JsonObject
                {
                    ["value"] = jsonValue,
                    ["name"] = jsonNameValue
                };
                if (descriptionAttr != null)
                {
                    valueObject["description"] = JsonValue.Create(descriptionAttr.Description);
                }
                jsonValuesArray.Add(valueObject);
            }

            var jsonNamesArray = new JsonArray();
            foreach (var name in names)
            {
                jsonNamesArray.Add(JsonValue.Create(name));
            }

            // see also: https://github.com/microsoft/OpenAPI/blob/main/extensions/index.md
            schema.Extensions.Add(OpenApiInfoExtensions.EnumNames, new JsonNodeExtension(jsonNamesArray));

            var xMsEnum = new JsonObject
            {
                ["name"] = JsonValue.Create(context.Type.Name),
                ["modelAsString"] = JsonValue.Create(false),
                ["values"] = jsonValuesArray
            };

            schema.Extensions[OpenApiInfoExtensions.MsEnum] = new JsonNodeExtension(xMsEnum);
        }
    }
}