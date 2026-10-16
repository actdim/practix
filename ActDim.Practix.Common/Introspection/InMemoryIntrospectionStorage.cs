using System;
using System.Collections.Concurrent;
using System.Reflection;
using ActDim.Practix.Abstractions.Patterns;

namespace ActDim.Practix.Introspection
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
            ArgumentNullException.ThrowIfNull(memberId, nameof(memberId));
            return _dictionary[memberId];
        }

        /// <summary>
        /// Attempts to retrieve introspection information for the specified member identifier.
        /// </summary>
        /// <param name="memberId">The member identifier.</param>
        /// <param name="info">The found introspection info, or null.</param>
        /// <returns>True if found; otherwise, false.</returns>
        public bool TryGet(IntrospectionMemberId memberId, out IntrospectionInfo info)
        {
            ArgumentNullException.ThrowIfNull(memberId, nameof(memberId));
            return _dictionary.TryGetValue(memberId, out info);
        }

        /// <summary>
        /// Gets or adds an <see cref="IntrospectionInfo"/> for the specified reflection member.
        /// </summary>
        /// <param name="m">The target member info.</param>
        /// <returns>The cached or created <see cref="IntrospectionInfo"/> instance.</returns>
        public IntrospectionInfo GetOrAdd(MemberInfo m)
        {
            ArgumentNullException.ThrowIfNull(m, nameof(m));
            var memberId = m.GetIntrospectionMemberId();
            return _dictionary.GetOrAdd(memberId, _ => m.GetIntrospectionInfo(false));
        }
    }
}
