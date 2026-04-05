using ActDim.Practix.Abstractions.Logging;

namespace ActDim.Practix.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public static class LoggingExtensions
    {
        // ToStatus
        public static IDictionary<string, object> ToLogStatus(this string status)
        {
            return new Dictionary<string, object>()
            {
                { ContextProperty.Status.ToString(), status }
            };
        }

        // ToProgress
        public static IDictionary<string, object> ToLogProgress(this string status, string progress)
        {
            return new Dictionary<string, object>()
            {
                { ContextProperty.Status.ToString(), status },
                { ContextProperty.Progress.ToString(), progress }
            };
        }
    }

}
