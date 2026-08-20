using System;

namespace ActDim.Observability
{
    /// <summary>
    /// Represents the execution status state of an operation (name, progress percentage, icon, and step indices).
    /// </summary>
    public readonly record struct ObservabilityStatus
    {
        /// <summary>
        /// Gets the name or description of the current status (e.g. "Downloading Dataset").
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Gets the progress percentage (0..100), or <see langword="null"/> if unquantified.
        /// </summary>
        public double? Progress { get; init; }

        /// <summary>
        /// Gets the visual icon or emoji associated with the status (e.g. "🚀", "⚡").
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// Gets the 1-based current step index, or <see langword="null"/> if not multi-step.
        /// </summary>
        public int? Step { get; init; }

        /// <summary>
        /// Gets the total number of steps in the sequence, or <see langword="null"/> if unquantified.
        /// </summary>
        public int? TotalSteps { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservabilityStatus"/> struct.
        /// </summary>
        public ObservabilityStatus(
            string? name = null,
            double? progress = null,
            string? icon = null,
            int? step = null,
            int? totalSteps = null)
        {
            Name = name;
            Progress = progress.HasValue ? Math.Clamp(progress.Value, 0.0, 100.0) : null;
            Icon = icon;
            Step = step;
            TotalSteps = totalSteps;
        }
    }
}
