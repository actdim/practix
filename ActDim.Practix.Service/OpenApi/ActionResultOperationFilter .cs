using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ActDim.Practix.Service.OpenApi
{
    /// <summary>
    /// ActionResultSupportOperationFilter
    /// </summary>
    public class ActionResultOperationFilter : IOperationFilter
    {
        public static readonly Type ActionResultInterfaceType = typeof(IActionResult);
        public static readonly Type ContentResultType = typeof(ContentResult);
        public static readonly Type FileResultType = typeof(FileResult);
        public static readonly Type GenericTaskType = typeof(Task<>);

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var returnType = context.MethodInfo.ReturnType;
            var isAsync = returnType.IsGenericType && GenericTaskType.Equals(returnType.GetGenericTypeDefinition());
            if (ActionResultInterfaceType.IsAssignableFrom(returnType)
                || (isAsync && ActionResultInterfaceType.IsAssignableFrom(returnType.GenericTypeArguments[0])))
            {
                // operation.Responses.Count == 0?
                if (context.ApiDescription.SupportedResponseTypes.Count == 0)
                {
                    operation.Responses.Clear();
                    // Content type metadata:
                    // var vediaTypeMaps = context.ApiDescription.SupportedResponseTypes
                    //    .SelectMany(rt => rt.ApiResponseFormats.Select(rf => new { Type = rt.Type, MediaType = rf.MediaType }));
                    // var producesAttrs = context.MethodInfo.GetCustomAttributes(typeof(ProducesAttribute))
                    //    .Cast<ProducesAttribute>().ToArray();
                    // var producesResponseTypeAttrs = context.MethodInfo.GetCustomAttributes(typeof(ProducesResponseTypeAttribute))
                    //    .Cast<ProducesResponseTypeAttribute>().ToArray();

                    // FileResult:
                    var contentType = "application/octet-stream"; // default type
                    operation.Responses.Add("200", new OpenApiResponse
                    {
                        Description = "File download",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            [contentType] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "binary"
                                }
                            }
                        }
                    });
                    // ContentResult:
                    // var contentType = "text/plain"; // default type
                    // operation.Responses["200"] = new OpenApiResponse
                    // {
                    //     Description = "Content Result",
                    //     Content = new Dictionary<string, OpenApiMediaType>
                    //     {
                    //         ["text/plain"] = new OpenApiMediaType
                    //         {
                    //             Schema = new OpenApiSchema
                    //             {
                    //                 Type = JsonSchemaType.String
                    //             }
                    //         }
                    //     }
                    // };
                }
            }
        }
    }
}
