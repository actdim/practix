
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public class ApiDocumentFilter : IDocumentFilter
    {
        private readonly AppSettings _appSettings;
        private readonly IList<Type> _extraTypes;
        public ApiDocumentFilter() : this(null, null)
        {
        }

        public ApiDocumentFilter(IOptions<AppSettings> options) : this(options, null)
        {
        }

        public ApiDocumentFilter(IList<Type> extraTypes) : this(null, extraTypes)
        {
        }

        public ApiDocumentFilter(IOptions<AppSettings> options, IList<Type> extraTypes)
        {
            _appSettings = options?.Value;
            _extraTypes = extraTypes;
        }

        private IEnumerable<Type> GetExtraTypes(DocumentFilterContext context)
        {
            // https://stackoverflow.com/questions/49006079/using-swashbuckle-for-asp-net-core-how-can-i-add-a-model-to-the-generated-model

            var actionDescriptors = context.ApiDescriptions.Select(apiDescription =>
                (ControllerActionDescriptor)apiDescription.ActionDescriptor);
            var srcTypes = actionDescriptors.Select(actionDescriptor => (MemberInfo)actionDescriptor.ControllerTypeInfo).Distinct().
                Concat(actionDescriptors.Select(actionDescriptor => actionDescriptor.MethodInfo).Distinct());
            var extraTypes = srcTypes.SelectMany(srcType => srcType.GetCustomAttributes<OpenApiAttribute>()).
                SelectMany(attr =>
                {
                    if (!attr.Exclude && attr != null && attr.ExtraTypes != null)
                    {
                        return attr.ExtraTypes;
                    }
                    return [];
                });
            if (_extraTypes != null)
            {
                extraTypes = extraTypes.Concat(_extraTypes);
            }
            return extraTypes.Distinct();
        }

        public void Apply(OpenApiDocument apiDoc, DocumentFilterContext context)
        {
            apiDoc.Info.SetSchemaPrefix(_appSettings.SchemaPrefix);

            // exluding action by route:
            // var routes = apiDoc.Paths.Where(...).ToList();
            // routes.ForEach(x => { apiDoc.Paths.Remove(x.Key); });

            if (apiDoc.Components == default)
            {
                apiDoc.Components = new OpenApiComponents();
            }
            if (apiDoc.Components.Schemas == default)
            {
                apiDoc.Components.Schemas = new Dictionary<string, IOpenApiSchema>();
            }

            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (apiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                {
                    var key = "/" + apiDescription.RelativePath.TrimEnd('/');
                    var pathItems = apiDoc.Paths[key];
                    var controllerRequiresAuthorize = actionDescriptor.ControllerTypeInfo.GetCustomAttributes<AuthorizeAttribute>().Any();
                    foreach (var operation in pathItems.Operations)
                    {
                        var methodRequiresAuthorize = actionDescriptor.MethodInfo.GetCustomAttributes<AuthorizeAttribute>().Any();
                        if (controllerRequiresAuthorize || methodRequiresAuthorize)
                        {
                            if (!actionDescriptor.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                                && !actionDescriptor.ControllerTypeInfo.GetCustomAttributes<AllowAnonymousAttribute>().Any())
                            {
                                if (operation.Value.Extensions == default)
                                {
                                    operation.Value.Extensions = new Dictionary<string, IOpenApiExtension>();
                                }
                                // new OpenApiBoolean(true)
                                operation.Value.Extensions[OpenApiInfoExtensions.Authorize] = new JsonNodeExtension(JsonValue.Create(true));
                            }
                        }
                    }
                }

                // exlude action directly:
                // if (Condition)
                // {
                //     var operation = (OperationType)Enum.Parse(typeof(OperationType), apiDescription.HttpMethod, true);
                //     var pathItem = apiDoc.Paths[key];
                //     pathItem.Operations.Remove(operation);
                //     // drop the entire route of there are no operations left
                //     if (!pathItem.Operations.Any())
                //     {
                //         apiDoc.Paths.Remove(key);
                //     }
                // }
            }

            foreach (var type in GetExtraTypes(context))
            {
                var schemaId = type.GetOpenApiSchemaId(_appSettings?.SchemaPrefix, _appSettings?.ClassPrefix);
                if (!context.SchemaRepository.TryLookupByType(type, out _))
                {
                    var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                    if (schema == default)
                    {
                        throw new InvalidOperationException($"Failed to register Open API schema type '{type.Name}'");
                    }

                    if (!context.SchemaRepository.Schemas.TryGetValue(schemaId, out _))
                    {
                        // you can take schemaId V2 and V3 from schemaRef.Reference.ReferenceV2, schemaRef.Reference.ReferenceV3
                        // context.SchemaRepository.TryLookupByType(type, out var schemaRef);
                        context.SchemaRepository.Schemas[schemaId] = schema;
                    }

                    if (!apiDoc.Components.Schemas.ContainsKey(schemaId))
                    {
                        if (schema is OpenApiSchema openApiSchema)
                        {
                            apiDoc.Components.Schemas[schemaId] = openApiSchema;
                        }
                        else
                        {
                            apiDoc.Components.Schemas[schemaId] = new OpenApiSchemaReference(schemaId);
                        }
                    }
                }
            }
        }
    }
}
