namespace ActDim.Practix.Abstractions.Logging
{
    /// <summary>
    /// https://sysdig-docs-pr-1867.onrender.com/en/docs/sysdig-monitor/events/severity-and-status/
    /// </summary>
    public enum EventSeverityLevel
    {
        /// <summary>
        /// Fatal
        /// </summary>
        Emergency = 0, // High severity
        Alert = 1, // High
        Critical = 2, // Medium
        Error = 3, // Medium
        Warning = 4, // Low
        Notice = 5, // Low
        Informational = 6, // None
        /// <summary>
        /// Trace/Verbose
        /// </summary>
        Debug = 7 // None
    }

    public enum EventStatus
    {
        Triggered = 0,
        Resolved = 1,
        Acknowledged = 2,
        Unacknowledged = 3,
        Silenced = 4
    }

    public enum IssueSeverityLevel
    {
        Showstopper = 0, // S0
        Blocker = 1, // S1
        Critical = 2, // S2
        Major = 3, // S3
        Minor = 4, // S4
        Trivial = 5, // S5
        Enhancement = 6 // S6
    }

    public enum IssuePriority
    {
        /// <summary>
        /// Critical
        /// </summary>
        Highest = 0, // P0
        High = 1, // P1
        /// <summary>
        /// Normal
        /// </summary>
        Medium = 2, // P2
        Low = 3, // P3
        Lowest = 4
    }

    public enum IssueType
    {
        Incident,
        Problem,
        Bug, // Deffect
        ChangeRequest,
        FeatureRequest,
        Enhancement,
        Task,
        Risk,
        RFI
    }
}
