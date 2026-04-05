namespace ActDim.Practix.Abstractions.Introspection
{
    public interface IBaseIntrospectionInfo
    {
        string Name { get; }

        string DisplayName { get; set; }

        object UserData { get; set; }
    }
}