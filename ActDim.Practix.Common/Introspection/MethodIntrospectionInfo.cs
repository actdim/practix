using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class MethodIntrospectionInfo : IntrospectionInfo, IMethodIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<MethodBase, MethodIntrospectionInfo> Cache = [];

        public bool IsConstructor { get; }

        public bool IsAbstract { get; }

        public bool IsVirtual { get; }

        public bool IsStatic { get; }

        public bool IsPublic { get; }

        public bool IsPrivate { get; }

        public bool IsProtected { get; }

        public bool IsInternal { get; }

        public bool IsProtectedInternal { get; }

        public bool IsPrivateProtected { get; }

        public bool IsGeneric { get; }

        public bool IsGenericDefinition { get; }

        public ITypeBaseIntrospectionInfo[] GenericParameters { get; }

        public ITypeBaseIntrospectionInfo[] GenericArguments { get; }

        public ITypeBaseIntrospectionInfo ReturnType { get; }

        public IParameterBaseIntrospectionInfo[] Parameters { get; }

        private static Type VoidType = typeof(void);

        private static ITypeBaseIntrospectionInfo VoidTypeIntrospectionInfo = new TypeBaseIntrospectionInfo(VoidType);

        public MethodIntrospectionInfo(MethodBase m) : base(m)
        {
            IsConstructor = m.IsConstructor;
            IsAbstract = m.IsAbstract;
            IsVirtual = m.IsVirtual;
            IsStatic = m.IsStatic;

            // Access levels
            IsPublic = m.IsPublic;
            IsPrivate = m.IsPrivate;
            IsProtected = m.IsFamily;
            IsInternal = m.IsAssembly;
            IsProtectedInternal = m.IsFamilyOrAssembly;
            IsPrivateProtected = m.IsFamilyAndAssembly;

            // Generic
            IsGeneric = m.IsGenericMethod;
            IsGenericDefinition = m.IsGenericMethodDefinition;

            if (m is MethodInfo mi)
            {
                ReturnType = (ITypeBaseIntrospectionInfo)mi.ReturnType.GetIntrospectionInfo(false);
            }
            else
            {
                ReturnType = VoidTypeIntrospectionInfo;
            }

            Parameters = [.. m.GetParameters().Select(x => x.GetIntrospectionInfo(false))];

            // x.IsGenericParameter == true
            GenericParameters = m.IsGenericMethodDefinition ? [.. m.GetGenericArguments().Select(x => (ITypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];

            GenericArguments = m.IsGenericMethod ? [.. m.GetGenericArguments().Where(x => !x.IsGenericParameter).Select(x => (ITypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))] : [];
        }
    }
}