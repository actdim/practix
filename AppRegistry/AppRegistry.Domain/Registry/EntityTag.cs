namespace ActDim.AppRegistry.Domain.Registry;

public class EntityTag
{
    public string EntityTypeCode { get; set; }

    public Guid EntityId { get; set; }

    public string Tag { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

