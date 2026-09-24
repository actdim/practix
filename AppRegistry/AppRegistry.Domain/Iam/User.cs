using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Domain.Iam;

public class User : IEntity<Guid>
{
    [Column("id")]
    public Guid Id { get; set; }

    public string ExternalId { get; set; }

    public string AuthProvider { get; set; } = "zitadel";

    public string OrgId { get; set; }

    public string Email { get; set; }

    public string DisplayName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public JsonDocument Settings { get; set; }

    public JsonDocument Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string Slug
    {
        get => ExternalId ?? Id.ToString();
        set => ExternalId = value;
    }

    public string Name
    {
        get => DisplayName ?? Slug;
        set => DisplayName = value;
    }

    public string Username
    {
        get => Slug;
        set => Slug = value;
    }

    public string GivenName
    {
        get => FirstName;
        set => FirstName = value;
    }

    public string FamilyName
    {
        get => LastName;
        set => LastName = value;
    }

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

