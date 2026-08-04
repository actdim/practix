namespace ActDim.Practix.Service.Settings
{
    public class RefreshTokenConfig
    {
        /// <summary>
        /// Refresh token lifetime in minutes
        /// </summary>
        public int LifetimeMinutes { get; set; } = 10080; // 7 Days
        // public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Issue a new refresh token when the current one is used
        /// </summary>
        public bool EnableRotation { get; set; } = true;

        /// <summary>
        /// Invalidate refresh token after successful use
        /// </summary>
        public bool RevokeAfterUse { get; set; } = true; // OneTimeUse

        /// <summary>
        /// Absolute session lifetime in minutes
        /// </summary>
        public int AbsoluteLifetimeMinutes { get; set; } = 525600; // 1 year
        // public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromDays(365);
    }
}
