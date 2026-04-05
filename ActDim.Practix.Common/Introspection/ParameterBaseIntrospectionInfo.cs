using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class ParameterBaseIntrospectionInfo : BaseIntrospectionInfo, IParameterBaseIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<ParameterInfo, ParameterBaseIntrospectionInfo> Cache = [];

        public ITypeBaseIntrospectionInfo ParameterType { get; protected set; }

        public int Position { get; protected set; }

        public ParameterBaseIntrospectionInfo(ParameterInfo p) : base()
        {
            Name = p.Name;

            DisplayName = p.Name;

            ParameterType = (ITypeBaseIntrospectionInfo)p.ParameterType.GetIntrospectionInfo(false);

            Position = p.Position;
        }
    }
}