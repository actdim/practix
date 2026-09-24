namespace ActDim.AppRegistry.Domain.Iam;

public class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
}

