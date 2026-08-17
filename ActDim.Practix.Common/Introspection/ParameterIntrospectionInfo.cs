using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Full introspection details DTO model for method parameters including enclosing member info.
    /// </summary>
    public class ParameterIntrospectionInfo : ParameterBaseIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<ParameterInfo, ParameterIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets enclosing member introspection details.</summary>
        public IntrospectionInfo Member { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ParameterIntrospectionInfo"/> class.</summary>
        public ParameterIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="ParameterIntrospectionInfo"/> class from a parameter info.</summary>
        /// <param name="p">The parameter info.</param>
        public ParameterIntrospectionInfo(ParameterInfo p) : base(p)
        {
            if (p.Member is MethodBase m)
            {
                Member = (IntrospectionInfo)m.GetIntrospectionInfo(false);
            }
            else if (p.Member is PropertyInfo pi)
            {
                Member = (IntrospectionInfo)pi.GetIntrospectionInfo(false);
            }
        }
    }
}
