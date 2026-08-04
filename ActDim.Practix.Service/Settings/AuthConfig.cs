namespace ActDim.Practix.Service.Settings
{
    public class AuthConfig
    {
        public AuthSchemeType? SchemeType { get; set; }

        public LocalAuthJwtConfig LocalJwt { get; set; }

        public OidcAuthConfig Oidc { get; set; }

        // public ApiKeyAuthConfig ApiKey { get; set; } // TBD

        public RefreshTokenConfig Refresh { get; set; }
    }
}
