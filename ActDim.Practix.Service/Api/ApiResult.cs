namespace ActDim.Practix.Service.Api
{
    /// <summary>
    /// Represents validation error details with string error codes.
    /// </summary>
    public class ValidationErrorInfo : ValidationErrorInfo<string>
    {
    }

    /// <summary>
    /// Represents structured error details with string error codes.
    /// </summary>
    public class ErrorInfo : ErrorInfo<string>
    {
    }

    /// <summary>
    /// Base API operation result with string error codes.
    /// </summary>
    public class BaseApiResult : BaseApiResult<string>
    {
    }

    /// <summary>
    /// General-purpose API operation result holding untyped object data.
    /// </summary>
    public class ApiResult : ApiResult<object>
    {
    }
}
