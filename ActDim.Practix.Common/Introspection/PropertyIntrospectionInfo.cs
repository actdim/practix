using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    [Serializable]
    public class PropertyIntrospectionInfo : IntrospectionInfo, IPropertyIntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<PropertyInfo, PropertyIntrospectionInfo> Cache = [];

        public ITypeBaseIntrospectionInfo PropertyType { get; }

        public bool IsStatic { get; }

        public bool IsPublic { get; }

        public bool IsPrivate { get; }

        public bool IsProtected { get; }

        public bool IsInternal { get; }

        public bool IsProtectedInternal { get; }

        public bool IsPrivateProtected { get; }

        public PropertyIntrospectionInfo(PropertyInfo p) : base(p)
        {
            PropertyType = (ITypeBaseIntrospectionInfo)p.PropertyType.GetIntrospectionInfo(false);

            var accessor = p.GetMethod ?? p.SetMethod;

            IsStatic = accessor?.IsStatic ?? false;
            
            if (accessor != null)
            {
                IsPublic = accessor.IsPublic;
                IsPrivate = accessor.IsPrivate;
                IsProtected = accessor.IsFamily;
                IsInternal = accessor.IsAssembly;
                IsProtectedInternal = accessor.IsFamilyOrAssembly;
                IsPrivateProtected = accessor.IsFamilyAndAssembly;
            }
            else
            {
                IsPublic = IsPrivate = IsProtected = IsInternal = IsProtectedInternal = IsPrivateProtected = false;
            }
        }
    }
}