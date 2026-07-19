namespace ActDim.AppRegistry.Domain.Core
{
    public interface IEntityRef<T>
    {
        T Id { get; set; }

        string EntityTypeCode { get; set; }

        /// <summary>
        /// Display name, human-readable
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// URL-safe, machine-readable, unique constraint
        /// </summary>
        string Slug { get; set; }
    }
}
