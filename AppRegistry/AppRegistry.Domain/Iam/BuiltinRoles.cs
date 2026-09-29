using System;

namespace ActDim.AppRegistry.Domain.Iam;

public static class BuiltinRoles
{
    public static readonly Guid SuperAdmin = new("08a31f2e-dfe6-58f5-bd20-3f90d3e5b473");
    public static readonly Guid Admin = new("12440c0b-f84f-5207-b3aa-693d9fa87a9b");
    public static readonly Guid User = new("3aff92c6-f64d-58ea-8330-ef496e6f4616");
    public static readonly Guid Guest = new("398618f5-ff78-54ac-ae27-1a8734465c46");
}
