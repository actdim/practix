using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    public class ParameterBaseIntrospectionInfo : BaseIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<ParameterInfo, ParameterBaseIntrospectionInfo> Cache = [];

        public TypeBaseIntrospectionInfo ParameterType { get; set; }

        public int Position { get; set; }

        public ParameterBaseIntrospectionInfo() { }

        public ParameterBaseIntrospectionInfo(ParameterInfo p) : base()
        {
            Name = p.Name;
            DisplayName = p.Name;
            ParameterType = (TypeBaseIntrospectionInfo)p.ParameterType.GetIntrospectionInfo(false);
            Position = p.Position;
        }
    }
}
