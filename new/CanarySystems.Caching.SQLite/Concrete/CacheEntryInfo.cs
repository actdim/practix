using System;

namespace CanarySystems.Caching.SQLite
{
    internal class CacheEntryInfo
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Key { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// ExpiresAt/ExpiresAtTime
        /// </summary>
        public DateTimeOffset ExpirationTime { get; set; }
    }
}
