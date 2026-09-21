namespace ActDim.Practix.Service.Settings
{
    public class AuthConfig
    {
        public AuthMode? Mode { get; set; }

        /// <summary>
        /// Requires HTTPS for authentication requests and metadata retrieval.
        /// Default is true.
        /// </summary>
        public bool RequireHttps { get; set; } = true;

        public LocalAuthJwtConfig LocalJwt { get; set; }

        public OidcAuthConfig Oidc { get; set; }

        public ApiKeyAuthConfig ApiKey { get; set; }

        public CookieAuthConfig Cookie { get; set; }

        public BasicAuthConfig Basic { get; set; }
    }
}
