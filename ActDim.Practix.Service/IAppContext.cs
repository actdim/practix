using ActDim.Practix.Service.Settings;
using System.Security.Claims;

namespace ActDim.Practix.Service
{
    public interface IAppContext
    {
        UserInfo CurrentUser { get; }

        Task SetIdentityAsync(ClaimsPrincipal principal, AuthConfig config);

        Task<string> GetAccessTokenAsync(UserInfo user, AuthConfig config, string audience = null);

        Task ValidateAccessTokenAsync(string token, AuthConfig config, IList<string> audiences = null);
    }
}
