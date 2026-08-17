using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Holds full reflection introspection details for a type including properties, fields, methods, and interfaces.
    /// </summary>
    public class TypeIntrospectionInfo : TypeBaseIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets property introspection details.</summary>
        public PropertyIntrospectionInfo[] Properties { get; set; }

        /// <summary>Gets or sets field introspection details.</summary>
        public FieldIntrospectionInfo[] Fields { get; set; }

        /// <summary>Gets or sets method introspection details.</summary>
        public MethodIntrospectionInfo[] Methods { get; set; }

        /// <summary>Gets or sets implemented interface introspection details.</summary>
        public TypeBaseIntrospectionInfo[] Interfaces { get; set; }

        /// <summary>Initializes a new instance of the <see cref="TypeIntrospectionInfo"/> class.</summary>
        public TypeIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="TypeIntrospectionInfo"/> class from a runtime type.</summary>
        /// <param name="t">The runtime type.</param>
        public TypeIntrospectionInfo(Type t) : base(t)
        {
            Properties = [.. t.GetProperties().Select(x => (PropertyIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Fields = [.. t.GetFields().Select(x => (FieldIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Methods = [.. t.GetMethods().Select(x => (MethodIntrospectionInfo)x.GetIntrospectionInfo(false))];
            Interfaces = [.. t.GetInterfaces().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))];
        }
    }
}
