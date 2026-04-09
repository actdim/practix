using Microsoft.OpenApi;

namespace ActDim.Practix.Service
{
    public interface IApiConfig
    {
        // EndpointTemplate
        // string RouteTemplate { get; set; }

        string TitleTemplate { get; set; }

        /// <summary>
        /// Doc(Info)
        /// </summary>
        OpenApiInfo Info { get; set; }

        /// <summary>
        ///
        /// </summary>
        bool Explorable { get; set; } // ShowInExplorer

        string[] Versions { get; set; }
    }

    public class ApiConfigOverride
    {
        public string Version { get; set; }

        public OpenApiInfo Info { get; set; }

        public bool Explorable { get; set; }
    }

    /// <summary>
    /// JwtBearerSettings
    /// </summary>
    public class JwtBearerConfig
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string Key { get; set; }
    }

    /// <summary>
    /// AuthType
    /// </summary>
    public enum AuthSchemeType
    {
        None = 0,
        JwtBearer = 1,
        ApiKey = 2,
        /// <summary>
        /// OAuth2 / OpenID Connect (SSO)
        /// </summary>
        OAuth2 = 3,
        RefreshToken = 4,
        Custom = 5
    };

    // TODO: support Azure AD etc
    // "AzureAd": {
    //   "Instance": "https://login.microsoftonline.com/",
    //   "Domain": "contoso.com",
    //   "TenantId": "your-tenant-id",
    //   "ClientId": "your-client-id",
    //   "CallbackPath": "/signin-oidc"
    // }

    public class RefreshTokenConfig
    {
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromDays(30);
        public bool EnableRotation { get; set; } = true;
        public bool OneTimeUse { get; set; } = true;
        public string Issuer { get; set; }
    }

    /// <summary>
    /// AuthSchemeConfigSettings
    /// </summary>
    public class AuthSchemeConfig
    {
        public JwtBearerConfig JwtBearer { get; set; }

        // public ApiKeyConfig ApiKey { get; set; }

        // public OAuth2Config OAuth2 { get; set; }

        public RefreshTokenConfig Refresh { get; set; }

        public object Custom { get; set; }

        public bool IsDefault { get; set; }
    }

    public class ApiConfig : IApiConfig
    {
        public string TitleTemplate { get; set; }

        public OpenApiInfo Info { get; set; }

        public bool Explorable { get; set; }

        public string AuthSchemeName { get; set; }

        public string[] Versions { get; set; }

        public ApiConfigOverride[] Overrides { get; set; }
    }

}
