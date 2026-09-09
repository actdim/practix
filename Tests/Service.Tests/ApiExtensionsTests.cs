using Asp.Versioning;
using ActDim.Practix.Service.OpenApi;
using Microsoft.OpenApi;
using Xunit;

namespace ActDim.Practix.Service.Tests
{
    public class ApiExtensionsTests
    {
        [Theory]
        [InlineData(1, 0, "1")]
        [InlineData(2, 5, "2.5")]
        [InlineData(0, 0, "0")]
        public void ApiVersion_GetName_FormatsCorrectly(int major, int minor, string expected)
        {
            var apiVersion = new ApiVersion(major, minor);
            var name = apiVersion.GetName();
            Assert.Equal(expected, name);
        }

        [Fact]
        public void OpenApiInfo_SetDocName_And_GetDocName_Roundtrip()
        {
            var docInfo = new OpenApiInfo();
            docInfo.SetDocName("test_v1");

            var result = docInfo.GetDocName();
            Assert.Equal("test_v1", result);
        }

        [Fact]
        public void OpenApiInfo_SetSchemaPrefix_And_GetSchemaPrefix_Roundtrip()
        {
            var docInfo = new OpenApiInfo();
            docInfo.SetSchemaPrefix("api_schema_prefix");

            var result = docInfo.GetSchemaPrefix();
            Assert.Equal("api_schema_prefix", result);
        }
    }
}

