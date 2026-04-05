using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class FieldIntrospectionInfo : IntrospectionInfo, IFieldIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<FieldInfo, FieldIntrospectionInfo> Cache = [];

        public ITypeBaseIntrospectionInfo FieldType { get; protected set; }

        public FieldIntrospectionInfo(FieldInfo f) : base(f)
        {
            FieldType = (ITypeBaseIntrospectionInfo)f.FieldType.GetIntrospectionInfo(false);
        }
    }
}