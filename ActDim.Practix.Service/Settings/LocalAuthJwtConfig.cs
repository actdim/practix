namespace ActDim.Practix.Service.Settings
{
    public class LocalAuthJwtConfig
    {
        public string Issuer { get; set; }

        /// <summary>
        /// Used when audience is not explicitly specified
        /// </summary>
        public string DefaultAudience { get; set; }

        /// <summary>
        /// Symmetric secret
        /// </summary>
        public string IssuerSigningKey { get; set; } = default!;

        /// <summary>
        /// HS256, HS384, HS512
        /// </summary>
        public string[] ValidAlgorithms { get; set; } = ["HS256"]; // IReadOnlyList?

        public TokenValidationConfig Validation { get; set; }
    }
}
