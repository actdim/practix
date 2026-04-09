using ActDim.Practix.Abstractions.Patterns;
using System.Collections.Concurrent;
using System.Reflection;

namespace ActDim.Practix.Common.Introspection
{
    public class InMemoryIntrospectionStorage : IProvider<IntrospectionInfo, IntrospectionMemberId>
    {
        private readonly ConcurrentDictionary<IntrospectionMemberId, IntrospectionInfo> _dictionary = [];

        public IntrospectionInfo Get(IntrospectionMemberId memberId)
        {
            return _dictionary[memberId];
        }

        public IntrospectionInfo GetOrAdd(MemberInfo m)
        {
            var memberId = m.GetIntrospectionMemberId();
            return _dictionary.GetOrAdd(memberId, memberId => m.GetIntrospectionInfo(false));
        }
    }
}
