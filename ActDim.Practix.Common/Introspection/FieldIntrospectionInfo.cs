using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Introspection information DTO model for reflection fields.
    /// </summary>
    public class FieldIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<FieldInfo, FieldIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets field type introspection details.</summary>
        public TypeBaseIntrospectionInfo FieldType { get; set; }

        /// <summary>Initializes a new instance of the <see cref="FieldIntrospectionInfo"/> class.</summary>
        public FieldIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="FieldIntrospectionInfo"/> class from a field info.</summary>
        /// <param name="f">The target field info.</param>
        public FieldIntrospectionInfo(FieldInfo f) : base(f)
        {
            FieldType = (TypeBaseIntrospectionInfo)f.FieldType.GetIntrospectionInfo(false);
        }
    }
}
