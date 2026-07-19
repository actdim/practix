namespace ActDim.Practix.Service.Settings
{
    public class AuthConfig
    {
        public LocalAuthJwtConfig LocalJwt { get; set; }

        public OidcAuthConfig Oidc { get; set; }

        // public ApiKeyAuthConfig ApiKey { get; set; } // TBD

        public RefreshTokenConfig Refresh { get; set; }
    }
}
