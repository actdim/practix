using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    public class ParameterIntrospectionInfo : ParameterBaseIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<ParameterInfo, ParameterIntrospectionInfo> Cache = [];

        public IntrospectionInfo Member { get; set; }

        public ParameterIntrospectionInfo() { }

        public ParameterIntrospectionInfo(ParameterInfo p) : base(p)
        {
            if (p.Member is MethodBase m)
                Member = (IntrospectionInfo)m.GetIntrospectionInfo(false);
            else if (p.Member is PropertyInfo pi)
                Member = (IntrospectionInfo)pi.GetIntrospectionInfo(false);
        }
    }
}
