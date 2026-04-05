using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class TypeBaseIntrospectionInfo : IntrospectionInfo, ITypeBaseIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeBaseIntrospectionInfo> Cache = [];
        public string FullName { get; }
        public string Namespace { get; }
        public string AssemblyQualifiedName { get; }
        public bool IsClass { get; }
        public bool IsInterface { get; }
        public bool IsAbstract { get; }
        public bool IsSealed { get; }
        public bool IsStatic { get; }
        public bool IsEnum { get; }
        public bool IsValueType { get; }
        public bool IsPrimitive { get; }
        public bool IsGeneric { get; }
        public bool IsGenericDefinition { get; }
        public bool IsNested { get; }
        public bool IsNotPublic { get; }
        public bool IsPublic { get; }
        public bool IsArray { get; }
        public bool IsPointer { get; }
        public bool IsByRef { get; }
        public ITypeBaseIntrospectionInfo ElementType { get; }
        public ITypeBaseIntrospectionInfo BaseType { get; }
        public ITypeBaseIntrospectionInfo[] GenericParameters { get; }
        public ITypeBaseIntrospectionInfo[] GenericArguments { get; }

        public TypeBaseIntrospectionInfo(Type t) : base(t)
        {
            // It doesn't make sense to use TypeInfo in modern dotnet
            // TODO: add ReflectionAssemblyMetadata, ReflectionModuleMetadata

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

            BaseType = t.BaseType != null ? (ITypeBaseIntrospectionInfo)t.BaseType.GetIntrospectionInfo(false) : null;
            ElementType = t.IsArray || t.IsPointer ? (ITypeBaseIntrospectionInfo)t.GetElementType().GetIntrospectionInfo(false) : null;

            // x.IsGenericParameter == true
            GenericParameters = t.IsGenericTypeDefinition ? [.. t.GetGenericArguments().Select(x => (ITypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];
            // Where(x => !x.IsGenericParameter)
            GenericArguments = t.IsGenericType ? [.. t.GetGenericArguments().Select(x => (ITypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];
            if (string.IsNullOrEmpty(t.FullName))
            {
                if (t.IsGenericTypeDefinition)
                {
                    FullName = $"{t.Name.Split('`').First()}<{string.Join(", ", GenericParameters.Select(x => x.Name))}>";
                }
                else if (t.IsGenericType)
                {
                    FullName = $"{t.Name.Split('`').First()}<{string.Join(", ", GenericArguments.Select(x => x.Name))}>";
                }
            }
        }
    }
}