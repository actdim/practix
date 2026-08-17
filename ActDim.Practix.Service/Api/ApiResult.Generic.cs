using System.Collections.Generic;

namespace ActDim.Practix.Service.Api
{
    /// <summary>
    /// Represents validation error details for an API response.
    /// </summary>
    /// <typeparam name="TCode">The error code type.</typeparam>
    public class ValidationErrorInfo<TCode>
    {
        /// <summary>
        /// Gets or sets the property or field path associated with the validation error.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the descriptive error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the specific validation error code.
        /// </summary>
        public TCode Code { get; set; }
    }

    /// <summary>
    /// Represents structured error details for an API response.
    /// </summary>
    /// <typeparam name="TCode">The error code type.</typeparam>
    public class ErrorInfo<TCode>
    {
        /// <summary>
        /// Gets or sets the descriptive error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the error type or category.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets extra details regarding the error.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets the call stack information when applicable.
        /// </summary>
        public string CallStack { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public TCode Code { get; set; }
    }

    /// <summary>
    /// Base envelope for API operation results containing status and diagnostic error collections.
    /// </summary>
    /// <typeparam name="TErrorCode">The error code type.</typeparam>
    public class BaseApiResult<TErrorCode>
    {
        /// <summary>
        /// Gets or sets whether the operation succeeded.
        /// </summary>
        public bool Ok { get; set; }

        /// <summary>
        /// Gets or sets the collection of errors encountered during the operation.
        /// </summary>
        public IList<ErrorInfo<TErrorCode>> Errors { get; set; }

        /// <summary>
        /// Gets or sets the collection of validation errors encountered during the operation.
        /// </summary>
        public IList<ValidationErrorInfo<TErrorCode>> ValidationErrors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApiResult{TErrorCode}"/> class.
        /// </summary>
        public BaseApiResult()
        {
            Errors = new List<ErrorInfo<TErrorCode>>();
            ValidationErrors = new List<ValidationErrorInfo<TErrorCode>>();
        }
    }

    /// <summary>
    /// Generic API result payload envelope with string error codes.
    /// </summary>
    /// <typeparam name="TData">The payload data type.</typeparam>
    public class ApiResult<TData> : BaseApiResult<string>
    {
        /// <summary>
        /// Gets or sets the result payload data.
        /// </summary>
        public TData Data { get; set; }
    }

    /// <summary>
    /// Generic API result payload envelope with custom error codes.
    /// </summary>
    /// <typeparam name="TData">The payload data type.</typeparam>
    /// <typeparam name="TErrorCode">The error code type.</typeparam>
    public class ApiResult<TData, TErrorCode> : BaseApiResult<TErrorCode>
    {
        /// <summary>
        /// Gets or sets the result payload data.
        /// </summary>
        public TData Data { get; set; }
    }
}
