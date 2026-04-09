using System;

namespace ActDim.Practix.Common.DataFormat
{
    public class DateTimeFormatConstants
    {
        /// <summary>
        /// transport-friendly (server independent), culture-invariant, wall clock
        /// </summary>
        public const string NaiveDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFF"; // FFF -> fff to make this part requiered

        /// <summary>
        /// Smart version of ISO 8601 (utc, universal, instant, absolute)
        /// </summary>
        public const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
    }
}
