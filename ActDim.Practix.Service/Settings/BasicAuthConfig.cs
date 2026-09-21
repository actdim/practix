namespace ActDim.Practix.Service.Settings
{
    /// <summary>
    /// Configuration for HTTP Basic authentication.
    /// </summary>
    public class BasicAuthConfig
    {
        /// <summary>
        /// Realm returned in the WWW-Authenticate header.
        /// </summary>
        public string Realm { get; set; } = "ActDim";

        /// <summary>
        /// Whether to suppress the WWW-Authenticate challenge header on 401 Unauthorized responses to avoid browser credential dialogs.
        /// </summary>
        public bool SuppressWwwAuthenticateHeader { get; set; } = true;
    }
}

