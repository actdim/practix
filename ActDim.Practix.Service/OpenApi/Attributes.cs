
namespace ActDim.Practix.Service.OpenApi
{
    // OpenApiIgnore
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ExcludeFromOpenApiAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class OpenApiAttribute : Attribute
    {
        public Type[] ExtraTypes { get; init; }

        // Ignore
        // ExcludeFromApi
        public bool Exclude { get; init; }
        
        public bool ExcludeFromExplorer { get; init; }
    }
}