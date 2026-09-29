namespace ActDim.AppRegistry.Domain.Core;

public class AuditLog
{
    public Guid Id { get; set; }

    public string EntityCode { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string[]? ChangedFields { get; set; }

    public string? ActorIp { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
