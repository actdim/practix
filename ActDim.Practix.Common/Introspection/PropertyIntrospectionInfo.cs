using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    public class PropertyIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<PropertyInfo, PropertyIntrospectionInfo> Cache = [];

        public TypeBaseIntrospectionInfo PropertyType { get; set; }

        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsInternal { get; set; }
        public bool IsProtectedInternal { get; set; }
        public bool IsPrivateProtected { get; set; }

        public PropertyIntrospectionInfo() { }

        public PropertyIntrospectionInfo(PropertyInfo p) : base(p)
        {
            PropertyType = (TypeBaseIntrospectionInfo)p.PropertyType.GetIntrospectionInfo(false);

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
        }
    }
}
