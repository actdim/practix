namespace ActDim.Practix.Service.Settings
{
    public class OidcAuthConfig
    {
        public string Authority { get; set; }

        /// <summary>
        /// Used when audience is not explicitly specified
        /// </summary>
        public string DefaultAudience { get; set; }

        public string Issuer { get; set; }

        /// <summary>
        /// Client used for OAuth/OIDC flows
        /// </summary>
        public OidcClientCredentials Client { get; set; }

        /// <summary>
        /// Optional client for RFC7662 introspection
        /// </summary>
        public OidcClientCredentials IntrospectionClient { get; set; }

        /// <summary>
        /// Symmetric secret
        /// </summary>
        public string IssuerSigningKey { get; set; }

        /// <summary>
        /// Symmetric: HS256, HS384, HS512
        /// Asymmetric (JWKS): RS256, RS384, RS512, PS256, PS384, PS512, ES256, ES384, ES512, EdDSA
        /// </summary>
        public string[] ValidAlgorithms { get; set; } = ["RS256", "PS256", "ES256"];

        public TokenValidationConfig Validation { get; set; }
    }
}
