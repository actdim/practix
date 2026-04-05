
using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class TypeIntrospectionInfo : TypeBaseIntrospectionInfo, ITypeIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeIntrospectionInfo> Cache = [];

        public IPropertyIntrospectionInfo[] Properties { get; }

        public IFieldIntrospectionInfo[] Fields { get; }

        public IMethodIntrospectionInfo[] Methods { get; }

        public ITypeBaseIntrospectionInfo[] Interfaces { get; }

        public TypeIntrospectionInfo(Type t) : base(t)
        {
            Properties = [.. t.GetProperties().Select(x => (IPropertyIntrospectionInfo)x.GetIntrospectionInfo(false))];

            Fields = [.. t.GetFields().Select(x => (IFieldIntrospectionInfo)x.GetIntrospectionInfo(false))];

            Methods = [.. t.GetMethods().Select(x => (IMethodIntrospectionInfo)x.GetIntrospectionInfo(false))];

            Interfaces = [.. t.GetInterfaces().Select(x => (ITypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))];
        }
    }

}