namespace ActDim.Practix.Service.Settings
{
    /// <summary>
    /// Configuration for Cookie-based authentication.
    /// </summary>
    public class CookieAuthConfig
    {
        /// <summary>
        /// Cookie name used for session authentication.
        /// </summary>
        public string CookieName { get; set; } = ".ActDim.Auth";

        /// <summary>
        /// Lifetime of the authentication cookie in minutes.
        /// </summary>
        public int LifetimeMinutes { get; set; } = 1440;

        /// <summary>
        /// Whether the cookie expiration should slide upon user activity.
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>
        /// Whether the cookie is inaccessible to client-side scripts.
        /// </summary>
        public bool HttpOnly { get; set; } = true;

        /// <summary>
        /// SameSite policy mode ("None", "Lax", "Strict").
        /// </summary>
        public string SameSite { get; set; } = "Lax";

        /// <summary>
        /// Whether the cookie is transmitted only over secure HTTPS connections.
        /// </summary>
        public bool SecureOnly { get; set; } = true;

        /// <summary>
        /// Optional cookie domain scope.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Cookie path scope.
        /// </summary>
        public string Path { get; set; } = "/";

        /// <summary>
        /// Redirect path for login requests.
        /// </summary>
        public string LoginPath { get; set; } = "/auth/login";

        /// <summary>
        /// Redirect path for logout requests.
        /// </summary>
        public string LogoutPath { get; set; } = "/auth/logout";

        /// <summary>
        /// Redirect path when access is denied.
        /// </summary>
        public string AccessDeniedPath { get; set; } = "/auth/access-denied";
    }
}

