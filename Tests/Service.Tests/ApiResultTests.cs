using System.Collections.Generic;
using ActDim.Practix.Service.Api;
using Xunit;

namespace ActDim.Practix.Service.Tests
{
    public class ApiResultTests
    {
        [Fact]
        public void BaseApiResult_InitializesEmptyErrorCollections()
        {
            var result = new BaseApiResult();

            Assert.False(result.Ok);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.ValidationErrors);
            Assert.Empty(result.ValidationErrors);
        }

        [Fact]
        public void ApiResult_Generic_HoldsDataAndSuccessStatus()
        {
            var result = new ApiResult<string>
            {
                Ok = true,
                Data = "payload-data"
            };

            Assert.True(result.Ok);
            Assert.Equal("payload-data", result.Data);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ApiResult_Untyped_CanStoreComplexPayload()
        {
            var payload = new Dictionary<string, int> { ["k1"] = 10, ["k2"] = 20 };
            var result = new ApiResult
            {
                Ok = true,
                Data = payload
            };

            Assert.True(result.Ok);
            Assert.Same(payload, result.Data);
        }

        [Fact]
        public void ErrorInfo_Properties_AreAssignable()
        {
            var error = new ErrorInfo
            {
                Code = "ERR_NOT_FOUND",
                Message = "Entity does not exist",
                Type = "NotFoundException",
                Details = "Lookup by ID 123",
                CallStack = "at Module.Method() in file.cs:line 1"
            };

            Assert.Equal("ERR_NOT_FOUND", error.Code);
            Assert.Equal("Entity does not exist", error.Message);
            Assert.Equal("NotFoundException", error.Type);
            Assert.Equal("Lookup by ID 123", error.Details);
            Assert.Equal("at Module.Method() in file.cs:line 1", error.CallStack);
        }

        [Fact]
        public void ValidationErrorInfo_Properties_AreAssignable()
        {
            var valError = new ValidationErrorInfo
            {
                Code = "VAL_REQUIRED",
                Path = "user.email",
                Message = "Email is required"
            };

            Assert.Equal("VAL_REQUIRED", valError.Code);
            Assert.Equal("user.email", valError.Path);
            Assert.Equal("Email is required", valError.Message);
        }

        [Fact]
        public void ApiResult_WithCustomErrorCode_WorksAsExpected()
        {
            var result = new ApiResult<int, int>
            {
                Ok = false,
                Data = -1
            };
            result.Errors.Add(new ErrorInfo<int>
            {
                Code = 404,
                Message = "Missing"
            });

            Assert.False(result.Ok);
            Assert.Equal(-1, result.Data);
            Assert.Single(result.Errors);
            Assert.Equal(404, result.Errors[0].Code);
        }
    }
}

