namespace ActDim.AppRegistry.Domain.Registry;

public class EntityTypeDef
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SchemaName { get; set; } = "public";

    public string? TableName { get; set; }

    public string PkColumn { get; set; } = "id";

    public string DisplayField { get; set; } = "name";

    public string? OwnerColumn { get; set; }

    public string? SoftDeleteColumn { get; set; }

    public bool IsSystem { get; set; }

    public bool IsAudited { get; set; } = true;

    public string? Icon { get; set; }

    public string? Category { get; set; }

    public bool IsNavVisible { get; set; } = true;

    public string DefaultSortField { get; set; } = "created_at";

    public string DefaultSortDir { get; set; } = "desc";

    public string Capabilities { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
}
