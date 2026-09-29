namespace ActDim.AppRegistry.Domain.Core;

public class MigrationDef
{
    public string Id { get; set; } = string.Empty;

    public string SubsystemCode { get; set; } = string.Empty;

    public string Checksum { get; set; } = string.Empty;

    public int? ExecutionTimeMs { get; set; }

    public DateTimeOffset AppliedAt { get; set; }

    public string AppliedBy { get; set; } = string.Empty;

    public bool IsBaseline { get; set; }
}
