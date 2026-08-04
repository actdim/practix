using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Net;
using System.Security.Cryptography;

namespace ActDim.Practix.Service.Settings
{
    /// <summary>
    /// Authentication scheme.
    /// </summary>
    public enum AuthSchemeType
    {
        None = 0,

        /// <summary>
        /// OpenID Connect (OIDC) / OAuth 2.0 authentication using an external Identity Provider.
        /// Keycloak, Azure AD, Auth0, Authentik, Authelia, ZITADEL, etc. (SSO)
        /// </summary>
        Oidc = 1,

        /// <summary>
        /// Local JWT authentication. Tokens are issued and validated by this application.
        /// </summary>
        LocalJwt = 2, // StandaloneJwt        

        /// <summary>
        /// Cookie-based authentication with server-side session (stateful).
        /// </summary>
        Cookie = 3,

        /// <summary>
        /// API Key authentication.
        /// </summary>
        ApiKey = 4,

        /// <summary>
        /// HTTP Basic authentication (username/password, legacy).
        /// </summary>
        Basic = 5
    }
}