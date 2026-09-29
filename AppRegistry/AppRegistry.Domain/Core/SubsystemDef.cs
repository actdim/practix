namespace ActDim.AppRegistry.Domain.Core;

public class SubsystemDef
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public int SchemaVersion { get; set; } = 1;

    public string Manifest { get; set; } = "{}";

    public DateTimeOffset InstalledAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
