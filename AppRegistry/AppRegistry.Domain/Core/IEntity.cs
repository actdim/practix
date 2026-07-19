namespace ActDim.AppRegistry.Domain.Core
{
    public interface IEntity<T> : IEntityRef<T>
    {
        DateTimeOffset CreatedAt { get; set; }

        DateTimeOffset UpdatedAt { get; set; }
    }
}
