using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace ActDim.AppRegistry.Domain.Core
{
    // Registry (directory/catalog) texonomy
    public class EntityTypeCode
    {
        public const string Org = "_org";

        public const string User = "_user";

        public const string Role = "_role";

        public const string Permission = "_permission";

        public const string Project = "_project";

        public static IReadOnlyDictionary<string, EntityType> Map = new ReadOnlyDictionary<string, EntityType>(
          new Dictionary<string, EntityType>()
          {
              [Org] = EntityType.Org,
              [User] = EntityType.User,
              [Role] = EntityType.Role,
              [Permission] = EntityType.Permission,
              [Project] = EntityType.Project
          });
    }

    public enum EntityType
    {
        [EnumMember(Value = EntityTypeCode.Org)]
        Org,
        [EnumMember(Value = EntityTypeCode.User)]
        User,
        [EnumMember(Value = EntityTypeCode.Role)]
        Role,
        [EnumMember(Value = EntityTypeCode.Permission)]
        Permission,
        [EnumMember(Value = EntityTypeCode.Project)]
        Project
    }
}

namespace ActDim.AppRegistry.Domain.Registry.Client
{

}
