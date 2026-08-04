namespace ActDim.Practix.Service.Settings
{
    public class TokenValidationConfig
    {
        public bool ValidateIssuer { get; set; } = true;
        
        public bool ValidateAudience { get; set; } = true;
        
        public bool ValidateLifetime { get; set; } = true;
        
        public bool ValidateSigningKey { get; set; } = true;

        public List<string> ValidAudiences { get; set; } = [];

        public int ClockSkewSeconds { get; set; } = 300; // 5 minutes
        // public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}
