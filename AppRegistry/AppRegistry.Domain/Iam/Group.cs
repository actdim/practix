using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Iam;

public class Group : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    public string AvatarUrl { get; set; }

    public bool IsPublic { get; set; }

    public JsonDocument Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string EntityTypeCode
    {
        get => Core.EntityTypeCode.Group;
        set
        {
            if (value != Core.EntityTypeCode.Group)
            {
                throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}

public class GroupMember
{
    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
