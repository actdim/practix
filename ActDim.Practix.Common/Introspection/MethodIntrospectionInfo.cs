using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Introspection information DTO model for reflection methods and constructors.
    /// </summary>
    public class MethodIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<MethodBase, MethodIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets whether the method is a constructor.</summary>
        public bool IsConstructor { get; set; }

        /// <summary>Gets or sets whether the method is abstract.</summary>
        public bool IsAbstract { get; set; }

        /// <summary>Gets or sets whether the method is virtual.</summary>
        public bool IsVirtual { get; set; }

        /// <summary>Gets or sets whether the method is static.</summary>
        public bool IsStatic { get; set; }

        /// <summary>Gets or sets whether the method is public.</summary>
        public bool IsPublic { get; set; }

        /// <summary>Gets or sets whether the method is private.</summary>
        public bool IsPrivate { get; set; }

        /// <summary>Gets or sets whether the method is protected.</summary>
        public bool IsProtected { get; set; }

        /// <summary>Gets or sets whether the method is internal.</summary>
        public bool IsInternal { get; set; }

        /// <summary>Gets or sets whether the method is protected internal.</summary>
        public bool IsProtectedInternal { get; set; }

        /// <summary>Gets or sets whether the method is private protected.</summary>
        public bool IsPrivateProtected { get; set; }

        /// <summary>Gets or sets whether the method is generic.</summary>
        public bool IsGeneric { get; set; }

        /// <summary>Gets or sets whether the method is a generic definition.</summary>
        public bool IsGenericDefinition { get; set; }

        /// <summary>Gets or sets generic parameter introspection info array.</summary>
        public TypeBaseIntrospectionInfo[] GenericParameters { get; set; }

        /// <summary>Gets or sets generic argument introspection info array.</summary>
        public TypeBaseIntrospectionInfo[] GenericArguments { get; set; }

        /// <summary>Gets or sets return type introspection info.</summary>
        public TypeBaseIntrospectionInfo ReturnType { get; set; }

        /// <summary>Gets or sets parameter introspection info array.</summary>
        public ParameterBaseIntrospectionInfo[] Parameters { get; set; }

        private static readonly TypeBaseIntrospectionInfo VoidTypeIntrospectionInfo = new(typeof(void));

        /// <summary>Initializes a new instance of the <see cref="MethodIntrospectionInfo"/> class.</summary>
        public MethodIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="MethodIntrospectionInfo"/> class from a method base.</summary>
        /// <param name="m">The target method info or constructor info.</param>
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
