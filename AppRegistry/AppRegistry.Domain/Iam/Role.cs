using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Iam;

[Table("roles")]
public class Role : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("is_system")]
    public bool IsSystem { get; set; }

    [Column("metadata")]
    public JsonDocument Metadata { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [NotMapped]
    public string Slug
    {
        get => Name;
        set => Name = value;
    }

    [NotMapped]
    public bool IsBuiltin
    {
        get => IsSystem;
        set => IsSystem = value;
    }

    [NotMapped]
    public string EntityTypeCode
    {
        get => Core.EntityTypeCode.Role;
        set
        {
            if (value != Core.EntityTypeCode.Role)
            {
                throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}
