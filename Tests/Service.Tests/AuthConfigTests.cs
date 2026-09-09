using System.Collections.Generic;
using ActDim.Practix.Service.Settings;
using Xunit;

namespace ActDim.Practix.Service.Tests
{
    public class AuthConfigTests
    {
        [Fact]
        public void LocalAuthJwtConfig_DefaultAlgorithm_IsHS256()
        {
            var config = new LocalAuthJwtConfig();

            Assert.NotNull(config.ValidAlgorithms);
            Assert.Contains("HS256", config.ValidAlgorithms);
        }

        [Fact]
        public void OidcAuthConfig_DefaultAlgorithms_IncludeExpectedSuites()
        {
            var config = new OidcAuthConfig();

            Assert.NotNull(config.ValidAlgorithms);
            Assert.Contains("RS256", config.ValidAlgorithms);
            Assert.Contains("PS256", config.ValidAlgorithms);
            Assert.Contains("ES256", config.ValidAlgorithms);
        }

        [Fact]
        public void RefreshTokenConfig_Defaults_AreConfigured()
        {
            var config = new RefreshTokenConfig();

            Assert.Equal(10080, config.LifetimeMinutes);
            Assert.True(config.EnableRotation);
            Assert.True(config.RevokeAfterUse);
            Assert.Equal(525600, config.AbsoluteLifetimeMinutes);
        }

        [Fact]
        public void TokenValidationConfig_Defaults_AreStrict()
        {
            var config = new TokenValidationConfig();

            Assert.True(config.ValidateIssuer);
            Assert.True(config.ValidateAudience);
            Assert.True(config.ValidateLifetime);
            Assert.True(config.ValidateSigningKey);
            Assert.Equal(300, config.ClockSkewSeconds);
            Assert.NotNull(config.ValidAudiences);
            Assert.Empty(config.ValidAudiences);
        }

        [Fact]
        public void AuthConfig_Properties_CanBeSet()
        {
            var auth = new AuthConfig
            {
                SchemeType = AuthSchemeType.LocalJwt,
                LocalJwt = new LocalAuthJwtConfig
                {
                    Issuer = "issuer",
                    DefaultAudience = "aud",
                    IssuerSigningKey = "symmetric-secret-key-for-test-32bytes"
                },
                Refresh = new RefreshTokenConfig
                {
                    LifetimeMinutes = 60
                }
            };

            Assert.Equal(AuthSchemeType.LocalJwt, auth.SchemeType);
            Assert.NotNull(auth.LocalJwt);
            Assert.Equal("issuer", auth.LocalJwt.Issuer);
            Assert.Equal(60, auth.Refresh.LifetimeMinutes);
        }
    }
}

