using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace ActDim.AppRegistry.Domain.Core;

public static class EntityTypeCode
{
    public const string User = "iam:user";
    public const string Group = "iam:group";
    public const string Role = "iam:role";
    public const string EntityPermission = "iam:entity_permission";
    public const string AuditLog = "actdim:audit_log";
    public const string VfsNode = "vfs:node";

    public static readonly IReadOnlyDictionary<string, EntityType> Map = new ReadOnlyDictionary<string, EntityType>(
        new Dictionary<string, EntityType>()
        {
            [User] = EntityType.User,
            [Group] = EntityType.Group,
            [Role] = EntityType.Role,
            [EntityPermission] = EntityType.EntityPermission,
            [AuditLog] = EntityType.AuditLog,
            [VfsNode] = EntityType.VfsNode
        });
}

public enum EntityType
{
    [EnumMember(Value = EntityTypeCode.User)]
    User,

    [EnumMember(Value = EntityTypeCode.Group)]
    Group,

    [EnumMember(Value = EntityTypeCode.Role)]
    Role,

    [EnumMember(Value = EntityTypeCode.EntityPermission)]
    EntityPermission,

    [EnumMember(Value = EntityTypeCode.AuditLog)]
    AuditLog,

    [EnumMember(Value = EntityTypeCode.VfsNode)]
    VfsNode
}
