using Microsoft.OpenApi;

namespace ActDim.Practix.Service.OpenApi
{
    public class OpenApiBoolean : IOpenApiExtension
    {
        private readonly bool _value;

        public OpenApiBoolean(bool value)
        {
            _value = value;
        }

        public bool Value => _value;

        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteValue(_value);
        }

        public static implicit operator OpenApiBoolean(bool value) => new OpenApiBoolean(value);
    }
}