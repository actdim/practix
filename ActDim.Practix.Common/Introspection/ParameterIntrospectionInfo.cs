using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class ParameterIntrospectionInfo : ParameterBaseIntrospectionInfo, IParameterIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<ParameterInfo, ParameterIntrospectionInfo> Cache = [];

        public IBaseIntrospectionInfo Member { get; }

        public ParameterIntrospectionInfo(ParameterInfo p) : base(p)
        {
            if (p.Member is MethodBase m) // including MethodInfo and ConstructorInfo
            {
                Member = m.GetIntrospectionInfo(false);
            }
            else if (p.Member is PropertyInfo pi)
            {
                Member = pi.GetIntrospectionInfo(false);
            }
        }
    }
}