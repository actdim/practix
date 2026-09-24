using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Registry;

public class Catalog : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    public Guid? ParentId { get; set; }

    public string Slug { get; set; }

    public string Path { get; set; }

    public string Name { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Description { get; set; }

    public JsonDocument Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string EntityTypeCode
    {
        get => Core.EntityTypeCode.Catalog;
        set
        {
            if (value != Core.EntityTypeCode.Catalog)
            {
                throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}

public class CatalogEntity
{
    public Guid CatalogId { get; set; }

    public string EntityTypeCode { get; set; }

    public Guid EntityId { get; set; }

    public DateTimeOffset AddedAt { get; set; }
}

