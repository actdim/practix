namespace ActDim.AppRegistry.Domain.Iam;

public class EntityPermission
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public string EntityCode { get; set; } = string.Empty;

    public bool CanCreate { get; set; }

    public string CanRead { get; set; } = "none";

    public string CanUpdate { get; set; } = "none";

    public string CanDelete { get; set; } = "none";

    public bool CanExport { get; set; }

    public string Metadata { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
