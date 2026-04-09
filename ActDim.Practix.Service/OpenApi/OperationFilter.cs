using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ActDim.Practix.Service.OpenApi
{
    public class OperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.OperationId = context.ApiDescription.GetOperationId();
        }
    }
}
