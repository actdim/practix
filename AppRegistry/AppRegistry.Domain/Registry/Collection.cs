using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Registry;

public class Collection : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? GroupId { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    public string AllowedEntityType { get; set; }

    public JsonDocument Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string EntityTypeCode
    {
        get => Core.EntityTypeCode.Collection;
        set
        {
            if (value != Core.EntityTypeCode.Collection)
            {
                throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}

public class CollectionEntity
{
    public Guid CollectionId { get; set; }

    public string EntityTypeCode { get; set; }

    public Guid EntityId { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset AddedAt { get; set; }
}

