using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    public class MethodIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<MethodBase, MethodIntrospectionInfo> Cache = [];

        public bool IsConstructor { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsInternal { get; set; }
        public bool IsProtectedInternal { get; set; }
        public bool IsPrivateProtected { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsGenericDefinition { get; set; }
        public TypeBaseIntrospectionInfo[] GenericParameters { get; set; }
        public TypeBaseIntrospectionInfo[] GenericArguments { get; set; }
        public TypeBaseIntrospectionInfo ReturnType { get; set; }
        public ParameterBaseIntrospectionInfo[] Parameters { get; set; }

        private static readonly TypeBaseIntrospectionInfo VoidTypeIntrospectionInfo = new TypeBaseIntrospectionInfo(typeof(void));

        public MethodIntrospectionInfo() { }

        public MethodIntrospectionInfo(MethodBase m) : base(m)
        {
            IsConstructor = m.IsConstructor;
            IsAbstract = m.IsAbstract;
            IsVirtual = m.IsVirtual;
            IsStatic = m.IsStatic;
            IsPublic = m.IsPublic;
            IsPrivate = m.IsPrivate;
            IsProtected = m.IsFamily;
            IsInternal = m.IsAssembly;
            IsProtectedInternal = m.IsFamilyOrAssembly;
            IsPrivateProtected = m.IsFamilyAndAssembly;
            IsGeneric = m.IsGenericMethod;
            IsGenericDefinition = m.IsGenericMethodDefinition;

            ReturnType = m is MethodInfo mi
                ? (TypeBaseIntrospectionInfo)mi.ReturnType.GetIntrospectionInfo(false)
                : VoidTypeIntrospectionInfo;

            Parameters = [.. m.GetParameters().Select(x => (ParameterBaseIntrospectionInfo)x.GetIntrospectionInfo(false))];

            GenericParameters = m.IsGenericMethodDefinition
                ? [.. m.GetGenericArguments().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))]
                : [];

            GenericArguments = m.IsGenericMethod
                ? [.. m.GetGenericArguments().Where(x => !x.IsGenericParameter).Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))]
                : [];
        }
    }
}
