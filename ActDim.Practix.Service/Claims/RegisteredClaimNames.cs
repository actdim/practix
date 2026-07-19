using System.Runtime.InteropServices;

namespace ActDim.Practix.Service;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct RegisteredClaimNames
{
    // public const string Role = "role";
    public const string Roles = "roles";
    public const string Permissions = "permissions";
    public const string TokenId = "tid"; // token_id
    public const string Scope = "scope";
    public const string Version = "ver";
}
