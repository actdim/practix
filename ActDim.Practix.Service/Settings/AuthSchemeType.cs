namespace ActDim.Practix.Service.Settings
{
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

}
