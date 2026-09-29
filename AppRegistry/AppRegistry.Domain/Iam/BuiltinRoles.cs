using System;

namespace ActDim.AppRegistry.Domain.Iam;

public static class BuiltinRoles
{
    public static readonly Guid Admin = new("d413f443-1c49-4d84-82fc-054b9e063c21");
    public static readonly Guid SuperAdmin = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid User = new("00000000-0000-0000-0000-000000000003");
    public static readonly Guid Guest = new("00000000-0000-0000-0000-000000000004");
}
