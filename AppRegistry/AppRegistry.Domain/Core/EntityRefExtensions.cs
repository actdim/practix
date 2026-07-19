namespace ActDim.AppRegistry.Domain.Core
{
    public static class EntityRefExtensions
    {
        public static string Key<T>(this IEntityRef<T> entityRef)
            => $"{entityRef.EntityTypeCode}/{entityRef.Id}";

        public static EntityType EntityType<T>(this IEntityRef<T> entityRef)
        {
            if (EntityTypeCode.Map.TryGetValue(entityRef.EntityTypeCode, out EntityType type))
            {
                return type;
            }

            throw new InvalidDataException();
        }
    }
}
