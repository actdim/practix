namespace ActDim.AppRegistry.Domain.Security
{
    public class TokenInfo
    {
        public string Token { get; set; }

        /// <summary>
        /// Sid
        /// </summary>
        public string UserId { get; set; }

        public string Username { get; set; }

        /// <summary>
        /// ExpiryTime
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public bool IsRevoked { get; set; }
    }
}