#nullable enable
using System.Collections.Generic;

namespace ActDim.Observability
{
    /// <summary>
    /// Represents a unified domain or telemetry event carrying a name and custom activity tags.
    /// </summary>
    public class LogEvent
    {
        public string Name { get; set; } = null!;
        public Dictionary<string, object> ActivityTags { get; set; } = [];

        public LogEvent()
        {
        }

        public LogEvent(string name, Dictionary<string, object>? activityTags = null)
        {
            Name = name;
            ActivityTags = activityTags ?? [];
        }
    }
}
