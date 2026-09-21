namespace ActDim.Practix.Service.Settings
{
    /// <summary>
    /// Configuration for API Key authentication.
    /// </summary>
    public class ApiKeyAuthConfig
    {
        /// <summary>
        /// HTTP header name containing the API key (e.g. "X-Api-Key" or "Authorization").
        /// </summary>
        public string HeaderName { get; set; } = "X-Api-Key";

        /// <summary>
        /// Optional prefix in the header value before the key (e.g. "ApiKey " or "Bearer ").
        /// </summary>
        public string HeaderPrefix { get; set; }

        /// <summary>
        /// Whether API key can be passed via query string parameter.
        /// Default is false for security best practices.
        /// </summary>
        public bool AllowQueryString { get; set; }

        /// <summary>
        /// Query string parameter name when query string lookup is enabled.
        /// </summary>
        public string QueryParameterName { get; set; } = "api_key";
    }
}

