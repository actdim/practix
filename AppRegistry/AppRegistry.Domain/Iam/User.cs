using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Iam;

[Table("users")]
public class User : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("external_id")]
    public string ExternalId { get; set; }

    [Column("auth_provider")]
    public string AuthProvider { get; set; } = "zitadel";

    [Column("org_id")]
    public string OrgId { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("avatar_url")]
    public string AvatarUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    [Column("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; set; }

    [Column("settings")]
    public JsonDocument Settings { get; set; }

    [Column("metadata")]
    public JsonDocument Metadata { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [NotMapped]
    public string Slug
    {
        get => ExternalId ?? Id.ToString();
        set => ExternalId = value;
    }

    [NotMapped]
    public string Name
    {
        get => DisplayName ?? Slug;
        set => DisplayName = value;
    }

    [NotMapped]
    public string Username
    {
        get => Slug;
        set => Slug = value;
    }

    [NotMapped]
    public string GivenName
    {
        get => FirstName;
        set => FirstName = value;
    }

    [NotMapped]
    public string FamilyName
    {
        get => LastName;
        set => LastName = value;
    }

    [NotMapped]
    public string EntityTypeCode
    {
        get => Core.EntityTypeCode.User;
        set
        {
            if (value != Core.EntityTypeCode.User)
            {
                throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}
