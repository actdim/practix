using System;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    /// <summary>
    /// DateTime extensions
    /// </summary>
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime date, DateTimeKind? kindOverride = default)
        {
            if (kindOverride != default)
            {
                date = DateTime.SpecifyKind(date, (DateTimeKind)kindOverride);
            }
            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }

        public static long ToUnixTimeMilliseconds(this DateTime date, DateTimeKind? kindOverride = default)
        {
            if (kindOverride != default)
            {
                date = DateTime.SpecifyKind(date, (DateTimeKind)kindOverride);
            }
            return ((DateTimeOffset)date).ToUnixTimeMilliseconds();
        }

        public static DateTime FromUnixTimeSeconds(double unixTimeSeconds, DateTimeKind kind = DateTimeKind.Utc)
        {
            var dt = DateTime.UnixEpoch.AddSeconds(unixTimeSeconds);
            return DateTime.SpecifyKind(dt, kind);
        }

        public static DateTime FromUnixTimeMilliseconds(double unixTimeMilliseconds, DateTimeKind kind = DateTimeKind.Utc)
        {
            var dt = DateTime.UnixEpoch.AddMilliseconds(unixTimeMilliseconds);
            return DateTime.SpecifyKind(dt, kind);
        }
    }
}
