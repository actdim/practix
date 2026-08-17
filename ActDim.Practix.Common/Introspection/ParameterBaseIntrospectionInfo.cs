using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Base introspection details DTO model for method parameters.
    /// </summary>
    public class ParameterBaseIntrospectionInfo : BaseIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<ParameterInfo, ParameterBaseIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets parameter type introspection details.</summary>
        public TypeBaseIntrospectionInfo ParameterType { get; set; }

        /// <summary>Gets or sets parameter position index.</summary>
        public int Position { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ParameterBaseIntrospectionInfo"/> class.</summary>
        public ParameterBaseIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="ParameterBaseIntrospectionInfo"/> class from a parameter info.</summary>
        /// <param name="p">The parameter info.</param>
        public ParameterBaseIntrospectionInfo(ParameterInfo p) : base()
        {
            Name = p.Name;
            DisplayName = p.Name;
            ParameterType = (TypeBaseIntrospectionInfo)p.ParameterType.GetIntrospectionInfo(false);
            Position = p.Position;
        }
    }
}
