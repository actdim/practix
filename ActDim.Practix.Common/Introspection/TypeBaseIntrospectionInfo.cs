using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    public class TypeBaseIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeBaseIntrospectionInfo> Cache = [];
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public string AssemblyQualifiedName { get; set; }
        public bool IsClass { get; set; }
        public bool IsInterface { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsStatic { get; set; }
        public bool IsEnum { get; set; }
        public bool IsValueType { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsGenericDefinition { get; set; }
        public bool IsNested { get; set; }
        public bool IsNotPublic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsArray { get; set; }
        public bool IsPointer { get; set; }
        public bool IsByRef { get; set; }
        public TypeBaseIntrospectionInfo ElementType { get; set; }
        public new TypeBaseIntrospectionInfo BaseType { get; set; }
        public TypeBaseIntrospectionInfo[] GenericParameters { get; set; }
        public TypeBaseIntrospectionInfo[] GenericArguments { get; set; }

        public TypeBaseIntrospectionInfo() { }

        public TypeBaseIntrospectionInfo(Type t) : base(t)
        {
            FullName = t.FullName ?? t.Name;
            Namespace = t.Namespace;
            AssemblyQualifiedName = t.AssemblyQualifiedName;
            IsClass = t.IsClass;
            IsInterface = t.IsInterface;
            IsAbstract = t.IsAbstract;
            IsSealed = t.IsSealed;
            IsStatic = t.IsAbstract && t.IsSealed;
            IsEnum = t.IsEnum;
            IsValueType = t.IsValueType;
            IsPrimitive = t.IsPrimitive;
            IsGeneric = t.IsGenericType;
            IsGenericDefinition = t.IsGenericTypeDefinition;
            IsNested = t.IsNested;
            IsNotPublic = t.IsNotPublic;
            IsArray = t.IsArray;
            IsPointer = t.IsPointer;
            IsByRef = t.IsByRef;

            BaseType = t.BaseType != null ? (TypeBaseIntrospectionInfo)t.BaseType.GetIntrospectionInfo(false) : null;
            ElementType = t.IsArray || t.IsPointer ? (TypeBaseIntrospectionInfo)t.GetElementType().GetIntrospectionInfo(false) : null;

            GenericParameters = t.IsGenericTypeDefinition ? [.. t.GetGenericArguments().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];
            GenericArguments = t.IsGenericType ? [.. t.GetGenericArguments().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];

            if (string.IsNullOrEmpty(t.FullName))
            {
                if (t.IsGenericTypeDefinition)
                    FullName = $"{t.Name.Split('`').First()}<{string.Join(", ", GenericParameters.Select(x => x.Name))}>";
                else if (t.IsGenericType)
                    FullName = $"{t.Name.Split('`').First()}<{string.Join(", ", GenericArguments.Select(x => x.Name))}>";
            }
        }
    }
}
