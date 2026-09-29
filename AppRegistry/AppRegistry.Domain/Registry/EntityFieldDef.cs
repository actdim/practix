namespace ActDim.AppRegistry.Domain.Registry;

public class EntityFieldDef
{
    public string EntityTypeCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ColumnName { get; set; }

    public string DisplayTitle { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string ScalarType { get; set; } = "String";

    public string InputType { get; set; } = "text";

    public bool IsPk { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsSecret { get; set; }

    public bool IsAudited { get; set; }

    public bool IsSearchable { get; set; }
 
    public bool IsFilterable { get; set; }

    public bool IsReadonly { get; set; }

    public bool ShowInList { get; set; } = true;

    public bool ShowInForm { get; set; } = true;

    public string Options { get; set; } = "[]";

    public string? HelpText { get; set; }

    public string? Placeholder { get; set; }

    public string? FkTargetEntityCode { get; set; }

    public int SortOrder { get; set; }

    public string Metadata { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
