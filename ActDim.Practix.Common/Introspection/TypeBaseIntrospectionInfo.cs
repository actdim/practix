using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    /// <summary>
    /// Holds base introspection details for a CLR <see cref="Type"/>.
    /// </summary>
    public class TypeBaseIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<Type, TypeBaseIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets the full type name.</summary>
        public string FullName { get; set; }

        /// <summary>Gets or sets the type namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets the assembly qualified name.</summary>
        public string AssemblyQualifiedName { get; set; }

        /// <summary>Gets or sets whether the type is a class.</summary>
        public bool IsClass { get; set; }

        /// <summary>Gets or sets whether the type is an interface.</summary>
        public bool IsInterface { get; set; }

        /// <summary>Gets or sets whether the type is abstract.</summary>
        public bool IsAbstract { get; set; }

        /// <summary>Gets or sets whether the type is sealed.</summary>
        public bool IsSealed { get; set; }

        /// <summary>Gets or sets whether the type is static.</summary>
        public bool IsStatic { get; set; }

        /// <summary>Gets or sets whether the type is an enum.</summary>
        public bool IsEnum { get; set; }

        /// <summary>Gets or sets whether the type is a value type.</summary>
        public bool IsValueType { get; set; }

        /// <summary>Gets or sets whether the type is a primitive.</summary>
        public bool IsPrimitive { get; set; }

        /// <summary>Gets or sets whether the type is generic.</summary>
        public bool IsGeneric { get; set; }

        /// <summary>Gets or sets whether the type is a generic definition.</summary>
        public bool IsGenericDefinition { get; set; }

        /// <summary>Gets or sets whether the type is a generic parameter.</summary>
        public bool IsGenericParameter { get; set; }

        /// <summary>Gets or sets whether the type is nested.</summary>
        public bool IsNested { get; set; }

        /// <summary>Gets or sets whether the type is non-public.</summary>
        public bool IsNotPublic { get; set; }

        /// <summary>Gets or sets whether the type is public.</summary>
        public bool IsPublic { get; set; }

        /// <summary>Gets or sets whether the type is an array.</summary>
        public bool IsArray { get; set; }

        /// <summary>Gets or sets whether the type is a pointer.</summary>
        public bool IsPointer { get; set; }

        /// <summary>Gets or sets whether the type is passed by reference.</summary>
        public bool IsByRef { get; set; }

        /// <summary>Gets or sets the array or pointer element type introspection info.</summary>
        public TypeBaseIntrospectionInfo ElementType { get; set; }

        /// <summary>Gets or sets the base type introspection info.</summary>
        public TypeBaseIntrospectionInfo BaseType { get; set; }

        /// <summary>Gets or sets generic parameter introspection info array.</summary>
        public TypeBaseIntrospectionInfo[] GenericParameters { get; set; }

        /// <summary>Gets or sets generic argument introspection info array.</summary>
        public TypeBaseIntrospectionInfo[] GenericArguments { get; set; }

        private readonly object _syncRoot = new();
        private bool _isInitialized;
        private int _initializingThreadId;

        /// <summary>Initializes a new instance of the <see cref="TypeBaseIntrospectionInfo"/> class.</summary>
        public TypeBaseIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="TypeBaseIntrospectionInfo"/> class from a runtime type.</summary>
        /// <param name="t">The runtime type.</param>
        public TypeBaseIntrospectionInfo(Type t)
        {
            ArgumentNullException.ThrowIfNull(t, nameof(t));
            EnsureInitialized(t);
        }

        internal void EnsureInitialized(Type t)
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_isInitialized)
                {
                    return;
                }

                if (_initializingThreadId == Environment.CurrentManagedThreadId)
                {
                    return;
                }

                _initializingThreadId = Environment.CurrentManagedThreadId;
                try
                {
                    Cache.AddOrUpdate(t, this);
                    Initialize(t);
                    _isInitialized = true;
                }
                finally
                {
                    _initializingThreadId = 0;
                }
            }
        }

        internal virtual void Initialize(Type t)
        {
            Name = t.Name;
            DisplayName = t.Name;
            MemberId = t.GetIntrospectionMemberId();
            MemberType = t.MemberType;
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
            IsGenericParameter = t.IsGenericParameter;
            IsNested = t.IsNested;
            IsNotPublic = t.IsNotPublic;
            IsPublic = t.IsPublic;
            IsArray = t.IsArray;
            IsPointer = t.IsPointer;
            IsByRef = t.IsByRef;

            DeclaringType = (TypeBaseIntrospectionInfo)t.DeclaringType?.GetIntrospectionInfo(false);
            ReflectedType = (TypeBaseIntrospectionInfo)t.ReflectedType?.GetIntrospectionInfo(false);

            BaseType = t.BaseType != null ? (TypeBaseIntrospectionInfo)t.BaseType.GetIntrospectionInfo(false) : null;
            ElementType = t.IsArray || t.IsPointer || t.IsByRef ? (TypeBaseIntrospectionInfo)t.GetElementType()?.GetIntrospectionInfo(false) : null;

            GenericParameters = t.IsGenericTypeDefinition
                ? [.. t.GetGenericArguments().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))]
                : [];
            GenericArguments = t.IsGenericType && !t.IsGenericTypeDefinition
                ? [.. t.GetGenericArguments().Select(x => (TypeBaseIntrospectionInfo)x.GetIntrospectionInfo(false))]
                : [];

            if (string.IsNullOrEmpty(t.FullName))
            {
                var prefix = string.IsNullOrEmpty(t.Namespace) ? string.Empty : $"{t.Namespace}.";
                var baseName = t.Name.Split('`').First();
                if (t.IsGenericTypeDefinition && GenericParameters.Length > 0)
                {
                    FullName = $"{prefix}{baseName}<{string.Join(", ", GenericParameters.Select(x => x.Name))}>";
                }
                else if (t.IsGenericType && GenericArguments.Length > 0)
                {
                    FullName = $"{prefix}{baseName}<{string.Join(", ", GenericArguments.Select(x => x.Name))}>";
                }
            }
        }
    }
}
