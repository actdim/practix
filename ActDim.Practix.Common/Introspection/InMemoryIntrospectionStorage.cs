using ActDim.Practix.Abstractions.Patterns;
using System.Collections.Concurrent;
using System.Reflection;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// In-memory thread-safe dictionary storage provider for reflection member <see cref="IntrospectionInfo"/>.
    /// </summary>
    public class InMemoryIntrospectionStorage : IProvider<IntrospectionInfo, IntrospectionMemberId>
    {
        private readonly ConcurrentDictionary<IntrospectionMemberId, IntrospectionInfo> _dictionary = [];

        /// <inheritdoc />
        public IntrospectionInfo Get(IntrospectionMemberId memberId)
        {
            return _dictionary[memberId];
        }

        /// <summary>
        /// Gets or adds an <see cref="IntrospectionInfo"/> for the specified reflection member.
        /// </summary>
        /// <param name="m">The target member info.</param>
        /// <returns>The cached or created <see cref="IntrospectionInfo"/> instance.</returns>
        public IntrospectionInfo GetOrAdd(MemberInfo m)
        {
            var memberId = m.GetIntrospectionMemberId();
            return _dictionary.GetOrAdd(memberId, memberId => m.GetIntrospectionInfo(false));
        }
    }
}
