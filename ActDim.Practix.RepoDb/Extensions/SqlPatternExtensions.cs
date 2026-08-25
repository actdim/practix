namespace ActDim.Practix.RepoDb.Extensions
{
    /// <summary>
    /// Extension methods for SQL pattern formatting and wildcard conversion.
    /// </summary>
    public static class SqlPatternExtensions
    {
        /// <summary>
        /// Normalizes wildcard patterns (converts user <c>*</c> to SQL <c>%</c>) with fallback default pattern <c>%</c>.
        /// </summary>
        public static string NormalizeSqlPattern(this string? pattern, string defaultPattern = "%")
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return defaultPattern;
            }

            return pattern.Replace('*', '%');
        }
    }
}
