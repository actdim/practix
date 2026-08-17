using System;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/> Unix timestamp conversions.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> to the number of seconds that have elapsed since 1970-01-01T00:00:00Z.
        /// </summary>
        /// <param name="date">The date time value.</param>
        /// <param name="kindOverride">An optional <see cref="DateTimeKind"/> override before conversion.</param>
        /// <returns>Unix timestamp in seconds.</returns>
        public static long ToUnixTimeSeconds(this DateTime date, DateTimeKind? kindOverride = default)
        {
            if (kindOverride != default)
            {
                date = DateTime.SpecifyKind(date, (DateTimeKind)kindOverride);
            }

            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to the number of milliseconds that have elapsed since 1970-01-01T00:00:00Z.
        /// </summary>
        /// <param name="date">The date time value.</param>
        /// <param name="kindOverride">An optional <see cref="DateTimeKind"/> override before conversion.</param>
        /// <returns>Unix timestamp in milliseconds.</returns>
        public static long ToUnixTimeMilliseconds(this DateTime date, DateTimeKind? kindOverride = default)
        {
            if (kindOverride != default)
            {
                date = DateTime.SpecifyKind(date, (DateTimeKind)kindOverride);
            }

            return ((DateTimeOffset)date).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Creates a <see cref="DateTime"/> from a Unix timestamp in seconds.
        /// </summary>
        /// <param name="unixTimeSeconds">The Unix timestamp in seconds.</param>
        /// <param name="kind">The target <see cref="DateTimeKind"/> (defaults to Utc).</param>
        /// <returns>The resulting <see cref="DateTime"/>.</returns>
        public static DateTime FromUnixTimeSeconds(double unixTimeSeconds, DateTimeKind kind = DateTimeKind.Utc)
        {
            var dt = DateTime.UnixEpoch.AddSeconds(unixTimeSeconds);
            return DateTime.SpecifyKind(dt, kind);
        }

        /// <summary>
        /// Creates a <see cref="DateTime"/> from a Unix timestamp in milliseconds.
        /// </summary>
        /// <param name="unixTimeMilliseconds">The Unix timestamp in milliseconds.</param>
        /// <param name="kind">The target <see cref="DateTimeKind"/> (defaults to Utc).</param>
        /// <returns>The resulting <see cref="DateTime"/>.</returns>
        public static DateTime FromUnixTimeMilliseconds(double unixTimeMilliseconds, DateTimeKind kind = DateTimeKind.Utc)
        {
            var dt = DateTime.UnixEpoch.AddMilliseconds(unixTimeMilliseconds);
            return DateTime.SpecifyKind(dt, kind);
        }
    }
}
