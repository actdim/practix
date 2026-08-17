using ActDim.AppRegistry.Domain.Core;
using ActDim.Practix.Service;
using ActDim.Practix.Service.Settings;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Superpower;
using System.Security.Claims;
using System.Text;

namespace ActDim.AppRegistry.Service;

public class AppContext : IAppContext
{
    public UserInfo CurrentUser { get; private set; }

    private readonly IAppRegistryService _appRegService;

    public AppContext(IAppRegistryService appRegService)
    {
        _appRegService = appRegService;
    }

    public async Task SetIdentityAsync(ClaimsPrincipal principal, AuthConfig config)
    {
        var id = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        // var username = principal.FindFirstValue(JwtRegisteredClaimNames.PreferredUsername);
        var user = await _appRegService.Users.GetByIdAsync(id);

        if (user == null)
        {
            throw new SecurityTokenException("User not found");
        }
        else
        {
            CurrentUser = new UserInfo(user.Id.ToString(), user.Username);
        }
    }

    public async Task<string> GetAccessTokenAsync(UserInfo user, AuthConfig config, string audience = null)
    {
        var now = DateTime.UtcNow;

        if (config.LocalJwt != null)
        {
            var localJwt = config.LocalJwt;

            var dbUser = await _appRegService.Users.GetByIdAsync(Guid.Parse(user.Id));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localJwt.IssuerSigningKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256); // ?

            // important: do not use ClaimTypes (MS ASP.NET specific) and System.IdentityModel.Tokens.Jwt namespace (old)

            // var claims = new Dictionary<string, object>
            // {
            //     ["claim1"] = "value",
            //     ["claim2"] = new[]
            //     {
            //         "value1",
            //         "value2"
            //     }
            // };

            // JwtRegisteredClaimNames — RFC 7519 (JWT)
            var tokenId = Guid.NewGuid().ToString();
            var claims = new List<Claim> {
                new(JwtRegisteredClaimNames.PreferredUsername, user.Username),
                new(JwtRegisteredClaimNames.Sub, dbUser.Id.ToString()),
                // JwtRegisteredClaimNames.Jti: JWT ID for blacklist/revocation etc
                new(JwtRegisteredClaimNames.Jti, tokenId),
                new(RegisteredClaimNames.TokenId, tokenId),
            };

            if (!string.IsNullOrEmpty(dbUser.Email))
            {
                // EmailVerified?
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, dbUser.Email));
            }
            if (!string.IsNullOrEmpty(dbUser.GivenName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, dbUser.GivenName));
            }
            if (!string.IsNullOrEmpty(dbUser.FamilyName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, dbUser.FamilyName));
            }

            claims.Add(new Claim(RegisteredClaimNames.Roles, "Admin")); // TODO: add roles etc

            // claims.Add(new Claim(RegisteredClaimNames.Roles, "Role")); // add multiple claims (values) with the same name

            // claims.Add(new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)); // issued at
            // claims.Add(new Claim(JwtRegisteredClaimNames.Exp, now.AddMinutes(60).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));        

            // claims.Add(new Claim(RegisteredClaimNames.Version, varsion));

            // JwtRegisteredClaimNames.Sid: session id
            // JwtRegisteredClaimNames.Azp: Authorized Party        

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = localJwt.Issuer,
                Audience = audience ?? localJwt.DefaultAudience,
                Subject = new ClaimsIdentity(claims),
                Expires = now.AddMinutes(60),
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler()
            {
                MapInboundClaims = false
            };
            var token = handler.CreateToken(tokenDescriptor);

            return token;
        }

        throw new NotSupportedException("Unsupported auth type");
    }

    public async Task ValidateAccessTokenAsync(string token, AuthConfig config, IList<string> audiences = null)
    {
        if (config.LocalJwt != null)
        {
            var localJwt = config.LocalJwt;
            try
            {
                var validationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = localJwt.Issuer,
                    ValidAudience = localJwt.DefaultAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localJwt.IssuerSigningKey)),
                    ValidateIssuer = localJwt.Validation.ValidateIssuer,
                    ValidateAudience = localJwt.Validation.ValidateAudience,
                    ValidateLifetime = localJwt.Validation.ValidateLifetime,
                    ValidateIssuerSigningKey = localJwt.Validation.ValidateSigningKey,
                    // LifetimeValidator
                    ClockSkew = TimeSpan.FromSeconds(localJwt.Validation.ClockSkewSeconds)
                };

                var tokenHandler = new JsonWebTokenHandler();

                var result = await tokenHandler.ValidateTokenAsync(
                    token,
                    validationParameters);

                if (!result.IsValid)
                {
                    throw result.Exception!;
                }

                var principal = result.ClaimsIdentity != null
                    ? new ClaimsPrincipal(result.ClaimsIdentity)
                    : throw new InvalidOperationException();

                var validAudiences = new List<string>(audiences ?? localJwt.Validation.ValidAudiences);
                if (audiences?.Count > 0)
                {
                    validAudiences.AddRange(audiences);
                }
                if (validAudiences.Count == 0)
                {
                    if (localJwt.Validation?.ValidAudiences?.Count > 0)
                    {
                        validAudiences.AddRange(localJwt.Validation.ValidAudiences);
                    }
                }
                if (validAudiences.Count == 0)
                {
                    validAudiences.Add(localJwt.DefaultAudience);
                }

                var userName = principal.FindFirst(JwtRegisteredClaimNames.PreferredUsername)?.Value;
                var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                var roles = principal.FindAll(RegisteredClaimNames.Roles)?.Select(c => c.Value);

                // JwtRegisteredClaimNames.Azp
                // JwtRegisteredClaimNames.Sid
                // RegisteredClaimNames.Scope

                // TODO: validate
            }
            catch (SecurityTokenExpiredException)
            {
                throw new UnauthorizedAccessException("Token expired");
            }
            catch (SecurityTokenValidationException ex)
            {
                throw new UnauthorizedAccessException($"Invalid token: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Error validating token: {ex.Message}");
            }
            return;
        }
        throw new NotSupportedException("Unsupported auth type");
    }
}
