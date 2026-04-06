using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    public class TypeIntrospectionInfo : TypeBaseIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeIntrospectionInfo> Cache = [];

        public PropertyIntrospectionInfo[] Properties { get; set; }

        public FieldIntrospectionInfo[] Fields { get; set; }

        public MethodIntrospectionInfo[] Methods { get; set; }

        public TypeBaseIntrospectionInfo[] Interfaces { get; set; }

        public TypeIntrospectionInfo() { }

        public TypeIntrospectionInfo(Type t) : base(t)
        {
            Properties = [.. t.GetProperties().Select(x => (PropertyIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Fields = [.. t.GetFields().Select(x => (FieldIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Methods = [.. t.GetMethods().Select(x => (MethodIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Interfaces = [.. t.GetInterfaces().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))];
        }
    }
}
