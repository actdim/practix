using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.Messaging
{
    /// <summary>
    /// DataValidationErrorInfo
    /// </summary>
    public class ValidationErrorInfo<TCode>
    {
        /// <summary>
        /// Property/Field
        /// </summary>
        public string Path { set; get; }

        public string Message { set; get; }

        public TCode Code { set; get; }
    }

    public class ErrorInfo<TCode>
    {
        /// <summary>
        /// Text
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Type { get; set; }

        public string Details { set; get; }

        public string CallStack { set; get; }

        public TCode Code { set; get; }

        // Severity?
    }

    public class BaseApiResult<TErrorCode>
    {
        public bool Ok { set; get; } // IsOk?

        public IList<ErrorInfo<TErrorCode>> Errors { set; get; }

        public IList<ValidationErrorInfo<TErrorCode>> ValidationErrors { set; get; }

        public BaseApiResult()
        {
            Errors = new List<ErrorInfo<TErrorCode>>();
            ValidationErrors = new List<ValidationErrorInfo<TErrorCode>>();
        }
    }

    public class ApiResult<TData>: BaseApiResult<string>
    {
        public TData Data { set; get; }
    }

    public class ApiResult<TData, TErrorCode> : BaseApiResult<TErrorCode>
    {
        public TData Data { set; get; }
    }
}
