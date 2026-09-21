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
                Mode = AuthMode.LocalJwt,
                RequireHttps = true,
                LocalJwt = new LocalAuthJwtConfig
                {
                    Issuer = "issuer",
                    DefaultAudience = "aud",
                    IssuerSigningKey = "symmetric-secret-key-for-test-32bytes",
                    Refresh = new RefreshTokenConfig
                    {
                        LifetimeMinutes = 60
                    }
                },
                ApiKey = new ApiKeyAuthConfig
                {
                    HeaderName = "X-Custom-Key",
                    AllowQueryString = true,
                    QueryParameterName = "token"
                },
                Cookie = new CookieAuthConfig
                {
                    CookieName = ".Custom.Cookie",
                    LifetimeMinutes = 120
                },
                Basic = new BasicAuthConfig
                {
                    Realm = "CustomRealm",
                    SuppressWwwAuthenticateHeader = false
                }
            };

            Assert.Equal(AuthMode.LocalJwt, auth.Mode);
            Assert.True(auth.RequireHttps);
            Assert.NotNull(auth.LocalJwt);
            Assert.Equal("issuer", auth.LocalJwt.Issuer);
            Assert.NotNull(auth.LocalJwt.Refresh);
            Assert.Equal(60, auth.LocalJwt.Refresh.LifetimeMinutes);
            Assert.Equal("X-Custom-Key", auth.ApiKey.HeaderName);
            Assert.True(auth.ApiKey.AllowQueryString);
            Assert.Equal("token", auth.ApiKey.QueryParameterName);
            Assert.Equal(".Custom.Cookie", auth.Cookie.CookieName);
            Assert.Equal(120, auth.Cookie.LifetimeMinutes);
            Assert.Equal("CustomRealm", auth.Basic.Realm);
            Assert.False(auth.Basic.SuppressWwwAuthenticateHeader);
        }

        [Fact]
        public void ApiKeyAuthConfig_Defaults_AreStandard()
        {
            var config = new ApiKeyAuthConfig();

            Assert.Equal("X-Api-Key", config.HeaderName);
            Assert.Null(config.HeaderPrefix);
            Assert.False(config.AllowQueryString);
            Assert.Equal("api_key", config.QueryParameterName);
        }

        [Fact]
        public void CookieAuthConfig_Defaults_AreStandard()
        {
            var config = new CookieAuthConfig();

            Assert.Equal(".ActDim.Auth", config.CookieName);
            Assert.Equal(1440, config.LifetimeMinutes);
            Assert.True(config.SlidingExpiration);
            Assert.True(config.HttpOnly);
            Assert.Equal("Lax", config.SameSite);
            Assert.True(config.SecureOnly);
            Assert.Equal("/", config.Path);
            Assert.Equal("/auth/login", config.LoginPath);
            Assert.Equal("/auth/logout", config.LogoutPath);
            Assert.Equal("/auth/access-denied", config.AccessDeniedPath);
        }

        [Fact]
        public void BasicAuthConfig_Defaults_AreStandard()
        {
            var config = new BasicAuthConfig();

            Assert.Equal("ActDim", config.Realm);
            Assert.True(config.SuppressWwwAuthenticateHeader);
        }
    }
}

