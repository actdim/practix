using ActDim.Practix.Abstractions.Introspection;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class BaseIntrospectionInfo : IBaseIntrospectionInfo
    {
        public string Name { get; protected set; }

        public string DisplayName { get; set; }

        public object UserData { get; set; }

        public BaseIntrospectionInfo()
        {

        }
    }
}