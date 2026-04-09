using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ActDim.Practix.Service.OpenApi
{
    public class ParameterOperationFilter : IOperationFilter
    {
        public static readonly Type ActionResultInterfaceType = typeof(IActionResult);
        public static readonly Type ContentResultType = typeof(ContentResult);
        public static readonly Type FileResultType = typeof(FileResult);
        public static readonly Type GenericTaskType = typeof(Task<>);
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParams = operation.Parameters
            .Where(p => p.Name.Equals("version", StringComparison.OrdinalIgnoreCase)
                     || p.Name.Equals(OpenApiInfoExtensions.ApiVersion, StringComparison.OrdinalIgnoreCase)
                     || p.Name.Equals("api-version", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var param in versionParams)
            {
                operation.Parameters.Remove(param);
            }
        }
    }
}
