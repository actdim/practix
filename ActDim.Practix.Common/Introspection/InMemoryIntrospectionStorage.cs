using ActDim.Practix.Abstractions.Introspection;
using ActDim.Practix.Abstractions.Patterns;
using System.Collections.Concurrent;
using System.Reflection;

namespace ActDim.Practix.Introspection
{
    // IIntrospectionInfoProvider
    public class InMemoryIntrospectionStorage : IProvider<IIntrospectionInfo, IntrospectionMemberId>
    {
        private readonly ConcurrentDictionary<IntrospectionMemberId, IIntrospectionInfo> _dictionary = [];

        public IIntrospectionInfo Get(IntrospectionMemberId memberId)
        {
            return _dictionary[memberId];
        }

        public IIntrospectionInfo GetOrAdd(MemberInfo m)
        {
            var memberId = m.GetIntrospectionMemberId();
            return _dictionary.GetOrAdd(memberId, memberId =>
            {
                return m.GetIntrospectionInfo(false);
            });
        }
    }
}