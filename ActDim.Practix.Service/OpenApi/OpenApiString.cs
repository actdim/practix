using Microsoft.OpenApi;

namespace ActDim.Practix.Service.OpenApi
{
    public class OpenApiString : IOpenApiExtension
    {
        private readonly string _value;

        public OpenApiString(string value)
        {
            _value = value;
        }

        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteValue(_value);
        }
    }
}
