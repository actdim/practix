namespace ActDim.Practix.Common.DataFormat
{
    /// <summary>
    /// Constants providing standardized ISO 8601 string formatting templates for <see cref="System.DateTime"/>.
    /// </summary>
    public class DateTimeFormatConstants
    {
        /// <summary>
        /// Transport-friendly (server independent), culture-invariant, wall clock naive date-time format template ("yyyy-MM-ddTHH:mm:ss.FFF").
        /// </summary>
        public const string NaiveDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFF"; // FFF -> fff to make this part required

        /// <summary>
        /// Smart version of ISO 8601 UTC instant (universal, absolute) date-time format template ("yyyy-MM-ddTHH:mm:ss.FFFFFFFK").
        /// </summary>
        public const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
    }
}
