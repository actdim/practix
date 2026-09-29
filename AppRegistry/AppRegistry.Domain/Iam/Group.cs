using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Iam;

[Table("groups")]
public class Group : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_user_id")]
    public Guid? OwnerUserId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("slug")]
    public string Slug { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("avatar_url")]
    public string AvatarUrl { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; }

    [Column("metadata")]
    public JsonDocument Metadata { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [NotMapped]
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

[Table("group_members")]
public class GroupMember
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("joined_at")]
    public DateTimeOffset JoinedAt { get; set; }
}
