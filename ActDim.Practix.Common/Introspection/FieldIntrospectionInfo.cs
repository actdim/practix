using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    public class FieldIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<FieldInfo, FieldIntrospectionInfo> Cache = [];

        public TypeBaseIntrospectionInfo FieldType { get; set; }

        public FieldIntrospectionInfo() { }

        public FieldIntrospectionInfo(FieldInfo f) : base(f)
        {
            FieldType = (TypeBaseIntrospectionInfo)f.FieldType.GetIntrospectionInfo(false);
        }
    }
}
